using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Domain.Enums;

namespace Project.Application.Requests.Queries.GetPendingRequests;

public record GetPendingRequestsQuery(int Page, int PageSize) : IRequest<PagedResult<RequestDto>>;

public class GetPendingRequestsQueryValidator : AbstractValidator<GetPendingRequestsQuery>
{
    public GetPendingRequestsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, PagedResult<RequestDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetPendingRequestsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<RequestDto>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        CurrentUserGuard.RequireRole(_currentUser, UserRole.Checker);

        var query = _db.WorkRequests
            .AsNoTracking()
            .Where(workRequest => workRequest.Status == RequestStatus.Pending)
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
