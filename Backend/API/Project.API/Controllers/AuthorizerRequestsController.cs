using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.Requests.Commands.ApproveRequest;
using Project.Application.Requests.Commands.RejectRequest;
using Project.Application.Requests.Queries.GetCheckedRequests;
using Project.Application.Requests.Queries.GetRequestById;
using Project.Domain.Enums;

namespace Project.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Authorizer))]
[Route("api/authorizer/requests")]
public class AuthorizerRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizerRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetCheckedRequestsQuery(page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<RequestDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ApproveRequestCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<RequestDto>> Reject(Guid id, [FromBody] RejectRequestBody? body, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new RejectRequestCommand(id, body?.Remarks), cancellationToken));
    }
}
