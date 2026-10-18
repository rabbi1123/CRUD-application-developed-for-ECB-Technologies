using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.API.Controllers;
using Project.Application.Auth.Commands.Login;
using Project.Application.Auth.Commands.RefreshToken;
using Project.Application.Auth.Commands.Register;
using Project.Application.Common;

namespace Project.API.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsCreated_WithTheMediatorResponse()
    {
        var response = new RegisterResponse(Guid.NewGuid(), "Ada Lovelace", "ada@ecb.test", "User");
        var mediator = new RecordingMediator { Response = response };
        var command = new RegisterCommand("Ada Lovelace", "ada@ecb.test", "Password123!");

        var result = await new AuthController(mediator).Register(command, CancellationToken.None);

        mediator.Request.Should().BeSameAs(command);
        var created = result.Result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithTheMediatorResponse()
    {
        var response = new AuthResponse("access", "refresh", DateTime.UtcNow, "User");
        var mediator = new RecordingMediator { Response = response };
        var command = new LoginCommand("user@ecb.test", "Password123!");

        var result = await new AuthController(mediator).Login(command, CancellationToken.None);

        mediator.Request.Should().BeSameAs(command);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task Refresh_ReturnsOk_WithTheMediatorResponse()
    {
        var response = new AuthResponse("access", "refresh", DateTime.UtcNow, "Checker");
        var mediator = new RecordingMediator { Response = response };
        var command = new RefreshTokenCommand("refresh-token");

        var result = await new AuthController(mediator).Refresh(command, CancellationToken.None);

        mediator.Request.Should().BeSameAs(command);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public void Endpoints_AreAnonymous_UnderTheAuthRoute()
    {
        var controller = typeof(AuthController);

        controller.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/auth");
        AssertAnonymousPost(controller, nameof(AuthController.Register), "register");
        AssertAnonymousPost(controller, nameof(AuthController.Login), "login");
        AssertAnonymousPost(controller, nameof(AuthController.Refresh), "refresh");
    }

    private static void AssertAnonymousPost(Type controller, string methodName, string template)
    {
        var method = controller.GetMethod(methodName)!;
        method.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        method.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be(template);
    }
}
