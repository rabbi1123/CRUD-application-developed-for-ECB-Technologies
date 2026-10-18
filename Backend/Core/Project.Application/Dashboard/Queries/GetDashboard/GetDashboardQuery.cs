using MediatR;
using Project.Application.Abstractions;
using Project.Application.Common;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Dashboard.Queries.GetDashboard;

public record GetDashboardQuery : IRequest<DashboardResponse>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly ICurrentUser _currentUser;

    public GetDashboardQueryHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        var message = _currentUser.Role switch
        {
            UserRole.User => "This is User Page",
            UserRole.Checker => "This is Checker Page",
            UserRole.Authorizer => "This is Authorizer Page",
            _ => throw new ForbiddenException("You do not have access to this action.")
        };

        return Task.FromResult(new DashboardResponse(message));
    }
}
