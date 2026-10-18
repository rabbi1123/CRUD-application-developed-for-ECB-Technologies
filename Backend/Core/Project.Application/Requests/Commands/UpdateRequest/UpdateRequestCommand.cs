using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Requests.Commands.UpdateRequest;

public record UpdateRequestCommand(Guid Id, string Title, string Description) : IRequest<RequestDto>;

public class UpdateRequestCommandValidator : AbstractValidator<UpdateRequestCommand>
{
    public UpdateRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(2000);
    }
}

public class UpdateRequestCommandHandler : IRequestHandler<UpdateRequestCommand, RequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RequestDto> Handle(UpdateRequestCommand request, CancellationToken cancellationToken)
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
            throw new ConflictException("Only pending or rejected requests can be updated.");
        }

        workRequest.Title = request.Title.Trim();
        workRequest.Description = request.Description.Trim();
        workRequest.UpdatedAt = DateTime.UtcNow;

        if (workRequest.Status == RequestStatus.Rejected)
        {
            workRequest.Status = RequestStatus.Pending;
            workRequest.CheckedByUserId = null;
            workRequest.CheckedAt = null;
            workRequest.CheckerRemarks = null;
            workRequest.AuthorizedByUserId = null;
            workRequest.AuthorizedAt = null;
            workRequest.AuthorizerRemarks = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return workRequest.ToDto();
    }
}
