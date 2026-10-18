using Project.Application.Dashboard.Queries.GetDashboard;

namespace Project.Application.Tests.Dashboard;

public class GetDashboardQueryTests
{
    [Theory]
    [InlineData(UserRole.User, "This is User Page")]
    [InlineData(UserRole.Checker, "This is Checker Page")]
    [InlineData(UserRole.Authorizer, "This is Authorizer Page")]
    public async Task Handle_ReturnsThePageForTheCurrentRole(UserRole role, string message)
    {
        var handler = new GetDashboardQueryHandler(new FakeCurrentUser { Role = role });

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.Message.Should().Be(message);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        var handler = new GetDashboardQueryHandler(new FakeCurrentUser { IsAuthenticated = false });

        var act = async () => await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Authentication is required.");
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotRecognized()
    {
        var handler = new GetDashboardQueryHandler(new FakeCurrentUser { Role = (UserRole)99 });

        var act = async () => await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }
}
