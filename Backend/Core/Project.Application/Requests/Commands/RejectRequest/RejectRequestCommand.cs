using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Requests.Commands.RejectRequest;

public record RejectRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDto>;

public class RejectRequestCommandValidator : AbstractValidator<RejectRequestCommand>
{
    public RejectRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Remarks).MaximumLength(1000);
    }
}

public class RejectRequestCommandHandler : IRequestHandler<RejectRequestCommand, RequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RejectRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RequestDto> Handle(RejectRequestCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        var workRequest = await _db.WorkRequests
            .SingleOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (workRequest is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        var remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        var now = DateTime.UtcNow;

        switch (_currentUser.Role)
        {
            case UserRole.Checker when workRequest.Status == RequestStatus.Pending:
                workRequest.Status = RequestStatus.Rejected;
                workRequest.CheckedByUserId = _currentUser.UserId;
                workRequest.CheckedAt = now;
                workRequest.CheckerRemarks = remarks;
                break;
            case UserRole.Authorizer when workRequest.Status == RequestStatus.Checked:
                workRequest.Status = RequestStatus.Rejected;
                workRequest.AuthorizedByUserId = _currentUser.UserId;
                workRequest.AuthorizedAt = now;
                workRequest.AuthorizerRemarks = remarks;
                break;
            case UserRole.Checker:
                throw new ConflictException("Only pending requests can be rejected by a checker.");
            case UserRole.Authorizer:
                throw new ConflictException("Only checked requests can be rejected by an authorizer.");
            default:
                throw new ForbiddenException("You do not have access to this action.");
        }

        workRequest.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return workRequest.ToDto();
    }
}
