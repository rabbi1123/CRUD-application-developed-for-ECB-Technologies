using FluentValidation;
using MediatR;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.Application.Requests.Commands.CreateRequest;

public record CreateRequestCommand(string Title, string Description) : IRequest<RequestDto>;

public class CreateRequestCommandValidator : AbstractValidator<CreateRequestCommand>
{
    public CreateRequestCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(2000);
    }
}

public class CreateRequestCommandHandler : IRequestHandler<CreateRequestCommand, RequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RequestDto> Handle(CreateRequestCommand request, CancellationToken cancellationToken)
    {
        CurrentUserGuard.RequireRole(_currentUser, UserRole.User);

        var now = DateTime.UtcNow;
        var workRequest = new WorkRequest
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = RequestStatus.Pending,
            CreatedByUserId = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.WorkRequests.Add(workRequest);
        await _db.SaveChangesAsync(cancellationToken);

        return workRequest.ToDto();
    }
}
