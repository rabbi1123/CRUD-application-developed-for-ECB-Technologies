using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Requests.Commands.ApproveRequest;

public record ApproveRequestCommand(Guid Id) : IRequest<RequestDto>;

public class ApproveRequestCommandValidator : AbstractValidator<ApproveRequestCommand>
{
    public ApproveRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public class ApproveRequestCommandHandler : IRequestHandler<ApproveRequestCommand, RequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ApproveRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RequestDto> Handle(ApproveRequestCommand request, CancellationToken cancellationToken)
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

        var now = DateTime.UtcNow;

        switch (_currentUser.Role)
        {
            case UserRole.Checker when workRequest.Status == RequestStatus.Pending:
                workRequest.Status = RequestStatus.Checked;
                workRequest.CheckedByUserId = _currentUser.UserId;
                workRequest.CheckedAt = now;
                break;
            case UserRole.Authorizer when workRequest.Status == RequestStatus.Checked:
                workRequest.Status = RequestStatus.Authorized;
                workRequest.AuthorizedByUserId = _currentUser.UserId;
                workRequest.AuthorizedAt = now;
                break;
            case UserRole.Checker:
                throw new ConflictException("Only pending requests can be checked.");
            case UserRole.Authorizer:
                throw new ConflictException("Only checked requests can be authorized.");
            default:
                throw new ForbiddenException("You do not have access to this action.");
        }

        workRequest.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return workRequest.ToDto();
    }
}
