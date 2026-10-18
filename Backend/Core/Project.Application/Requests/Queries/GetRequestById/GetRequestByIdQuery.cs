using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Requests.Queries.GetRequestById;

public record GetRequestByIdQuery(Guid Id) : IRequest<RequestDto>;

public class GetRequestByIdQueryValidator : AbstractValidator<GetRequestByIdQuery>
{
    public GetRequestByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public class GetRequestByIdQueryHandler : IRequestHandler<GetRequestByIdQuery, RequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetRequestByIdQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RequestDto> Handle(GetRequestByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        var workRequest = await _db.WorkRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (workRequest is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        switch (_currentUser.Role)
        {
            case UserRole.User when workRequest.CreatedByUserId != _currentUser.UserId:
                throw new NotFoundException("Request was not found.");
            case UserRole.Checker when workRequest.Status != RequestStatus.Pending:
                throw new ConflictException("Only pending requests are visible to a checker.");
            case UserRole.Authorizer when workRequest.Status != RequestStatus.Checked:
                throw new ConflictException("Only checked requests are visible to an authorizer.");
            case UserRole.User:
            case UserRole.Checker:
            case UserRole.Authorizer:
                return workRequest.ToDto();
            default:
                throw new ForbiddenException("You do not have access to this action.");
        }
    }
}
