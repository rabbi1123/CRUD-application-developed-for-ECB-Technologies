using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Requests.Commands.DeleteRequest;

public record DeleteRequestCommand(Guid Id) : IRequest;

public class DeleteRequestCommandValidator : AbstractValidator<DeleteRequestCommand>
{
    public DeleteRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public class DeleteRequestCommandHandler : IRequestHandler<DeleteRequestCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteRequestCommand request, CancellationToken cancellationToken)
    {
        CurrentUserGuard.RequireRole(_currentUser, UserRole.User);

        var workRequest = await _db.WorkRequests
            .SingleOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (workRequest is null || workRequest.CreatedByUserId != _currentUser.UserId)
        {
            throw new NotFoundException("Request was not found.");
        }

        if (workRequest.Status is not (RequestStatus.Pending or RequestStatus.Rejected))
        {
            throw new ConflictException("Only pending or rejected requests can be deleted.");
        }

        _db.WorkRequests.Remove(workRequest);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
