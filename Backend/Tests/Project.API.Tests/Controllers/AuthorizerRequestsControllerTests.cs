using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Controllers;
using Project.Application.Common;
using Project.Application.Requests.Commands.ApproveRequest;
using Project.Application.Requests.Commands.RejectRequest;
using Project.Application.Requests.Queries.GetCheckedRequests;
using Project.Application.Requests.Queries.GetRequestById;
using Project.Domain.Enums;

namespace Project.API.Tests.Controllers;

public class AuthorizerRequestsControllerTests
{
    [Fact]
    public async Task List_SendsTheCheckedQuery()
    {
        var page = new PagedResult<RequestDto>([], 3, 5, 0);
        var mediator = new RecordingMediator { Response = page };

        var result = await new AuthorizerRequestsController(mediator).List(3, 5, CancellationToken.None);

        mediator.Request.Should().Be(new GetCheckedRequestsQuery(3, 5));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [Fact]
    public async Task Get_SendsTheRouteId()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator { Response = SampleRequest(id) };

        await new AuthorizerRequestsController(mediator).Get(id, CancellationToken.None);

        mediator.Request.Should().Be(new GetRequestByIdQuery(id));
    }

    [Fact]
    public async Task Approve_SendsTheApproveCommand()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator { Response = SampleRequest(id) };

        await new AuthorizerRequestsController(mediator).Approve(id, CancellationToken.None);

        mediator.Request.Should().Be(new ApproveRequestCommand(id));
    }

    [Fact]
    public async Task Reject_SendsNullRemarks_WhenTheBodyIsMissing()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator { Response = SampleRequest(id) };

        await new AuthorizerRequestsController(mediator).Reject(id, null, CancellationToken.None);

        mediator.Request.Should().Be(new RejectRequestCommand(id, null));
    }

    [Fact]
    public void Controller_RequiresTheAuthorizerRole()
    {
        var controller = typeof(AuthorizerRequestsController);
        controller.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/authorizer/requests");
        controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be(nameof(UserRole.Authorizer));
    }

    private static RequestDto SampleRequest(Guid id) =>
        new(id, "Title", "Description", "Checked", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, null, null, null);
}
