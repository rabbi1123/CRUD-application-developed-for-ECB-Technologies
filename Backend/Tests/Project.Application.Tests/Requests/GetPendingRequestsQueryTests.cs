using FluentValidation.TestHelper;
using Project.Application.Requests.Queries.GetPendingRequests;

namespace Project.Application.Tests.Requests;

public class GetPendingRequestsQueryTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyPendingRequests_NewestFirst()
    {
        await using var db = TestDbContext.Create();
        var ownerId = Guid.NewGuid();
        await db.AddRequestAsync(ownerId, createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), title: "Older pending");
        await db.AddRequestAsync(ownerId, createdAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), title: "Newer pending");
        await db.AddRequestAsync(ownerId, RequestStatus.Checked, title: "Checked");
        await db.AddRequestAsync(ownerId, RequestStatus.Rejected, title: "Rejected");
        var handler = new GetPendingRequestsQueryHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var result = await handler.Handle(new GetPendingRequestsQuery(1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Title).Should().Equal("Newer pending", "Older pending");
        result.Items.Should().OnlyContain(item => item.Status == nameof(RequestStatus.Pending));
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Authorizer)]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotChecker(UserRole role)
    {
        await using var db = TestDbContext.Create();
        var handler = new GetPendingRequestsQueryHandler(db, new FakeCurrentUser { Role = role });

        var act = async () => await handler.Handle(new GetPendingRequestsQuery(1, 10), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        await using var db = TestDbContext.Create();
        var handler = new GetPendingRequestsQueryHandler(db, new FakeCurrentUser { IsAuthenticated = false, Role = UserRole.Checker });

        var act = async () => await handler.Handle(new GetPendingRequestsQuery(1, 10), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Authentication is required.");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    public void Validate_RejectsPagingOutsideTheAllowedRange(int page, int pageSize)
    {
        var result = new GetPendingRequestsQueryValidator().TestValidate(new GetPendingRequestsQuery(page, pageSize));

        result.ShouldHaveAnyValidationError();
    }
}
