using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Controllers;
using Project.Application.Common;
using Project.Application.Dashboard.Queries.GetDashboard;

namespace Project.API.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task Get_ReturnsTheDashboardFromTheMediator()
    {
        var response = new DashboardResponse("This is User Page");
        var mediator = new RecordingMediator { Response = response };

        var result = await new DashboardController(mediator).Get(CancellationToken.None);

        mediator.Request.Should().BeOfType<GetDashboardQuery>();
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public void Controller_RequiresAuthentication()
    {
        var controller = typeof(DashboardController);
        controller.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/dashboard");
        controller.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().BeNull();
        controller.GetMethod(nameof(DashboardController.Get))!.GetCustomAttribute<HttpGetAttribute>().Should().NotBeNull();
    }
}
