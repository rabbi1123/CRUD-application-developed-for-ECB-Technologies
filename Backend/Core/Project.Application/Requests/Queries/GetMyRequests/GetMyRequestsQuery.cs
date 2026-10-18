using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Domain.Enums;

namespace Project.Application.Requests.Queries.GetMyRequests;

public record GetMyRequestsQuery(int Page, int PageSize) : IRequest<PagedResult<RequestDto>>;

public class GetMyRequestsQueryValidator : AbstractValidator<GetMyRequestsQuery>
{
    public GetMyRequestsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetMyRequestsQueryHandler : IRequestHandler<GetMyRequestsQuery, PagedResult<RequestDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyRequestsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<RequestDto>> Handle(GetMyRequestsQuery request, CancellationToken cancellationToken)
    {
        CurrentUserGuard.RequireRole(_currentUser, UserRole.User);

        var query = _db.WorkRequests
            .AsNoTracking()
            .Where(workRequest => workRequest.CreatedByUserId == _currentUser.UserId)
            .OrderByDescending(workRequest => workRequest.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RequestDto>(
            items.Select(workRequest => workRequest.ToDto()).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }
}
