using FluentValidation.TestHelper;
using Project.Application.Requests.Queries.GetMyRequests;

namespace Project.Application.Tests.Requests;

public class GetMyRequestsQueryTests
{
    [Fact]
    public async Task Handle_ReturnsTheCallersRequests_NewestFirst()
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        await db.AddRequestAsync(currentUser.UserId, createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), title: "Oldest");
        await db.AddRequestAsync(currentUser.UserId, createdAt: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), title: "Newest");
        await db.AddRequestAsync(currentUser.UserId, createdAt: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), title: "Middle");
        await db.AddRequestAsync(Guid.NewGuid(), title: "Someone else");
        var handler = new GetMyRequestsQueryHandler(db, currentUser);

        var firstPage = await handler.Handle(new GetMyRequestsQuery(1, 2), CancellationToken.None);
        var secondPage = await handler.Handle(new GetMyRequestsQuery(2, 2), CancellationToken.None);

        firstPage.TotalCount.Should().Be(3);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(2);
        firstPage.Items.Select(item => item.Title).Should().Equal("Newest", "Middle");
        secondPage.Items.Select(item => item.Title).Should().Equal("Oldest");
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyPage_WhenTheCallerHasNoRequests()
    {
        await using var db = TestDbContext.Create();
        var handler = new GetMyRequestsQueryHandler(db, new FakeCurrentUser());

        var result = await handler.Handle(new GetMyRequestsQuery(1, 10), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(UserRole.Checker)]
    [InlineData(UserRole.Authorizer)]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotUser(UserRole role)
    {
        await using var db = TestDbContext.Create();
        var handler = new GetMyRequestsQueryHandler(db, new FakeCurrentUser { Role = role });

        var act = async () => await handler.Handle(new GetMyRequestsQuery(1, 10), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validate_RejectsPagingOutsideTheAllowedRange(int page, int pageSize)
    {
        var result = new GetMyRequestsQueryValidator().TestValidate(new GetMyRequestsQuery(page, pageSize));

        result.ShouldHaveAnyValidationError();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 100)]
    public void Validate_AcceptsPagingInsideTheAllowedRange(int page, int pageSize)
    {
        var result = new GetMyRequestsQueryValidator().TestValidate(new GetMyRequestsQuery(page, pageSize));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
