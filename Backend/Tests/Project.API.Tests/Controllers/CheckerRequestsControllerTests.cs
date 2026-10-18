using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Controllers;
using Project.Application.Common;
using Project.Application.Requests.Commands.ApproveRequest;
using Project.Application.Requests.Commands.RejectRequest;
using Project.Application.Requests.Queries.GetPendingRequests;
using Project.Application.Requests.Queries.GetRequestById;
using Project.Domain.Enums;

namespace Project.API.Tests.Controllers;

public class CheckerRequestsControllerTests
{
    [Fact]
    public async Task List_SendsThePendingQuery()
    {
        var page = new PagedResult<RequestDto>([], 1, 10, 0);
        var mediator = new RecordingMediator { Response = page };

        var result = await new CheckerRequestsController(mediator).List();

        mediator.Request.Should().Be(new GetPendingRequestsQuery(1, 10));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [Fact]
    public async Task Get_SendsTheRouteId()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator { Response = SampleRequest(id) };

        await new CheckerRequestsController(mediator).Get(id, CancellationToken.None);

        mediator.Request.Should().Be(new GetRequestByIdQuery(id));
    }

    [Fact]
    public async Task Approve_SendsTheApproveCommand()
    {
        var id = Guid.NewGuid();
        var dto = SampleRequest(id);
        var mediator = new RecordingMediator { Response = dto };

        var result = await new CheckerRequestsController(mediator).Approve(id, CancellationToken.None);

        mediator.Request.Should().Be(new ApproveRequestCommand(id));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Reject_SendsRemarks_WhenABodyIsProvided()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator { Response = SampleRequest(id) };

        await new CheckerRequestsController(mediator).Reject(id, new RejectRequestBody("Needs work"), CancellationToken.None);

        mediator.Request.Should().Be(new RejectRequestCommand(id, "Needs work"));
    }

    [Fact]
    public async Task Reject_SendsNullRemarks_WhenTheBodyIsMissing()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator { Response = SampleRequest(id) };

        var result = await new CheckerRequestsController(mediator).Reject(id, null, CancellationToken.None);

        mediator.Request.Should().Be(new RejectRequestCommand(id, null));
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Controller_RequiresTheCheckerRole()
    {
        var controller = typeof(CheckerRequestsController);
        controller.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/checker/requests");
        controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be(nameof(UserRole.Checker));
    }

    private static RequestDto SampleRequest(Guid id) =>
        new(id, "Title", "Description", "Pending", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, null, null, null, null);
}
