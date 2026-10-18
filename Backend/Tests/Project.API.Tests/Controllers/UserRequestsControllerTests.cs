using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Controllers;
using Project.Application.Common;
using Project.Application.Requests.Commands.CreateRequest;
using Project.Application.Requests.Commands.DeleteRequest;
using Project.Application.Requests.Commands.UpdateRequest;
using Project.Application.Requests.Queries.GetMyRequests;
using Project.Application.Requests.Queries.GetRequestById;
using Project.Domain.Enums;

namespace Project.API.Tests.Controllers;

public class UserRequestsControllerTests
{
    [Fact]
    public async Task List_SendsTheRequestedPage()
    {
        var page = new PagedResult<RequestDto>([], 2, 25, 0);
        var mediator = new RecordingMediator { Response = page };

        var result = await new UserRequestsController(mediator).List(2, 25, CancellationToken.None);

        mediator.Request.Should().Be(new GetMyRequestsQuery(2, 25));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [Fact]
    public async Task List_UsesTheDefaultPage()
    {
        var mediator = new RecordingMediator { Response = new PagedResult<RequestDto>([], 1, 10, 0) };

        await new UserRequestsController(mediator).List();

        mediator.Request.Should().Be(new GetMyRequestsQuery(1, 10));
    }

    [Fact]
    public async Task Get_SendsTheRouteId()
    {
        var id = Guid.NewGuid();
        var dto = SampleRequest(id);
        var mediator = new RecordingMediator { Response = dto };

        var result = await new UserRequestsController(mediator).Get(id, CancellationToken.None);

        mediator.Request.Should().Be(new GetRequestByIdQuery(id));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtTheGetAction()
    {
        var dto = SampleRequest(Guid.NewGuid());
        var mediator = new RecordingMediator { Response = dto };
        var command = new CreateRequestCommand("Title", "Description");

        var result = await new UserRequestsController(mediator).Create(command, CancellationToken.None);

        mediator.Request.Should().BeSameAs(command);
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UserRequestsController.Get));
        created.RouteValues!["id"].Should().Be(dto.Id);
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Update_SendsTheRouteIdWithTheBody()
    {
        var id = Guid.NewGuid();
        var dto = SampleRequest(id);
        var mediator = new RecordingMediator { Response = dto };

        var result = await new UserRequestsController(mediator).Update(
            id,
            new UpdateRequestBody("Title", "Description"),
            CancellationToken.None);

        mediator.Request.Should().Be(new UpdateRequestCommand(id, "Title", "Description"));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var mediator = new RecordingMediator();

        var result = await new UserRequestsController(mediator).Delete(id, CancellationToken.None);

        mediator.Request.Should().Be(new DeleteRequestCommand(id));
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void Controller_RequiresTheUserRole()
    {
        var controller = typeof(UserRequestsController);
        controller.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/user/requests");
        controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be(nameof(UserRole.User));
    }

    private static RequestDto SampleRequest(Guid id) =>
        new(id, "Title", "Description", "Pending", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, null, null, null, null);
}
