using FluentValidation.TestHelper;
using Project.Application.Requests.Queries.GetCheckedRequests;

namespace Project.Application.Tests.Requests;

public class GetCheckedRequestsQueryTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyCheckedRequests_NewestFirst()
    {
        await using var db = TestDbContext.Create();
        var ownerId = Guid.NewGuid();
        await db.AddRequestAsync(ownerId, RequestStatus.Checked, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), "Older checked");
        await db.AddRequestAsync(ownerId, RequestStatus.Checked, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), "Newer checked");
        await db.AddRequestAsync(ownerId, RequestStatus.Pending, title: "Pending");
        await db.AddRequestAsync(ownerId, RequestStatus.Authorized, title: "Authorized");
        var handler = new GetCheckedRequestsQueryHandler(db, new FakeCurrentUser { Role = UserRole.Authorizer });

        var result = await handler.Handle(new GetCheckedRequestsQuery(1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Title).Should().Equal("Newer checked", "Older checked");
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Checker)]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotAuthorizer(UserRole role)
    {
        await using var db = TestDbContext.Create();
        var handler = new GetCheckedRequestsQueryHandler(db, new FakeCurrentUser { Role = role });

        var act = async () => await handler.Handle(new GetCheckedRequestsQuery(1, 10), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 100)]
    public void Validate_AcceptsPagingInsideTheAllowedRange(int page, int pageSize)
    {
        var result = new GetCheckedRequestsQueryValidator().TestValidate(new GetCheckedRequestsQuery(page, pageSize));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_RejectsAPageSizeAboveTheMaximum()
    {
        var result = new GetCheckedRequestsQueryValidator().TestValidate(new GetCheckedRequestsQuery(1, 101));

        result.ShouldHaveValidationErrorFor(query => query.PageSize);
    }
}
