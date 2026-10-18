using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Project.API.Middleware;
using Project.Application.Exceptions;

namespace Project.API.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [MemberData(nameof(MappedExceptions))]
    public async Task Invoke_WritesAProblem_ForKnownApplicationExceptions(
        Exception exception,
        int statusCode,
        string title,
        string detail)
    {
        var result = await InvokeAsync(_ => throw exception, Environments.Production);

        result.StatusCode.Should().Be(statusCode);
        result.ContentType.Should().Be("application/problem+json");
        result.Logger.ErrorCount.Should().Be(0);
        using var document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("status").GetInt32().Should().Be(statusCode);
        document.RootElement.GetProperty("title").GetString().Should().Be(title);
        document.RootElement.GetProperty("detail").GetString().Should().Be(detail);
        document.RootElement.GetProperty("type").GetString().Should().Be($"https://httpstatuses.com/{statusCode}");
        document.RootElement.GetProperty("instance").GetString().Should().Be("/api/dashboard");
    }

    [Fact]
    public async Task Invoke_IncludesFieldErrors_ForValidationExceptions()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Email", "Email is invalid."),
            new ValidationFailure("Password", "Password is required.")
        ]);

        var result = await InvokeAsync(_ => throw exception, Environments.Production);

        using var document = JsonDocument.Parse(result.Body);
        var errors = document.RootElement.GetProperty("errors");
        errors.GetProperty("Email").EnumerateArray().Select(item => item.GetString())
            .Should().Equal("Email is required.", "Email is invalid.");
        errors.GetProperty("Password").EnumerateArray().Select(item => item.GetString())
            .Should().Equal("Password is required.");
    }

    [Fact]
    public async Task Invoke_HidesUnexpectedErrors_OutsideDevelopment()
    {
        var result = await InvokeAsync(_ => throw new InvalidOperationException("database password"), Environments.Production);

        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Logger.ErrorCount.Should().Be(1);
        using var document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("title").GetString().Should().Be("Server error.");
        document.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        result.Body.Should().NotContain("database password");
    }

    [Fact]
    public async Task Invoke_ShowsUnexpectedErrorDetail_InDevelopment()
    {
        var result = await InvokeAsync(_ => throw new InvalidOperationException("database password"), Environments.Development);

        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Logger.ErrorCount.Should().Be(1);
        using var document = JsonDocument.Parse(result.Body);
        document.RootElement.GetProperty("detail").GetString().Should().Be("database password");
    }

    [Fact]
    public async Task Invoke_LeavesASuccessfulResponseUnchanged()
    {
        var result = await InvokeAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }, Environments.Production);

        result.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        result.Body.Should().BeEmpty();
        result.Logger.ErrorCount.Should().Be(0);
    }

    public static IEnumerable<object[]> MappedExceptions()
    {
        yield return [new UnauthorizedException("Authentication is required."), StatusCodes.Status401Unauthorized, "Unauthorized.", "Authentication is required."];
        yield return [new ForbiddenException("You do not have access to this action."), StatusCodes.Status403Forbidden, "Forbidden.", "You do not have access to this action."];
        yield return [new NotFoundException("Request was not found."), StatusCodes.Status404NotFound, "Not found.", "Request was not found."];
        yield return [new ConflictException("An account with this email already exists."), StatusCodes.Status409Conflict, "Conflict.", "An account with this email already exists."];
    }

    private static async Task<ProblemResult> InvokeAsync(RequestDelegate next, string environmentName)
    {
        var logger = new ListLogger<ExceptionHandlingMiddleware>();
        var middleware = new ExceptionHandlingMiddleware(next, logger, new TestHostEnvironment { EnvironmentName = environmentName });
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/dashboard";
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return new ProblemResult(context.Response.StatusCode, context.Response.ContentType, body, logger);
    }

    private sealed record ProblemResult(int StatusCode, string? ContentType, string Body, ListLogger<ExceptionHandlingMiddleware> Logger);
}
