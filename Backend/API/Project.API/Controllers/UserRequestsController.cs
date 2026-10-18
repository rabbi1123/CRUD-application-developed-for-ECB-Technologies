using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.Requests.Commands.CreateRequest;
using Project.Application.Requests.Commands.DeleteRequest;
using Project.Application.Requests.Commands.UpdateRequest;
using Project.Application.Requests.Queries.GetMyRequests;
using Project.Application.Requests.Queries.GetRequestById;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.User))]
[Route("api/user/requests")]
public class UserRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetMyRequestsQuery(page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Update(Guid id, UpdateRequestBody body, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new UpdateRequestCommand(id, body.Title, body.Description), cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRequestCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdateRequestBody(string Title, string Description);
