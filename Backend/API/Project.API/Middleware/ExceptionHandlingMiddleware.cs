using System.Text.Json;
using Project.Application.Exceptions;

namespace Project.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed.", exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized.", exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden.", exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found.", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict.", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error.",
                _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        var problem = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{status}",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail,
            ["instance"] = context.Request.Path.Value
        };

        if (exception is ValidationException validationException)
        {
            problem["errors"] = validationException.Errors;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
