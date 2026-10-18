using FluentValidation.TestHelper;
using Project.Application.Requests.Queries.GetRequestById;

namespace Project.Application.Tests.Requests;

public class GetRequestByIdQueryTests
{
    [Fact]
    public async Task Handle_ReturnsTheOwnersRequest_ForAnyStatus()
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        var existing = await db.AddRequestAsync(currentUser.UserId, RequestStatus.Authorized, title: "Mine");
        var handler = new GetRequestByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        result.Title.Should().Be("Mine");
        result.Status.Should().Be(nameof(RequestStatus.Authorized));
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenAUserRequestsSomeoneElsesRequest()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ReturnsAPendingRequest_ToAChecker()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid(), title: "Waiting");
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var result = await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        result.Title.Should().Be("Waiting");
        result.Status.Should().Be(nameof(RequestStatus.Pending));
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenACheckerRequestsARequestThatIsNotPending()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Rejected);
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var act = async () => await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only pending requests are visible to a checker.");
    }

    [Fact]
    public async Task Handle_ReturnsACheckedRequest_ToAnAuthorizer()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Checked, title: "Ready");
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser { Role = UserRole.Authorizer });

        var result = await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        result.Title.Should().Be("Ready");
        result.Status.Should().Be(nameof(RequestStatus.Checked));
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenAnAuthorizerRequestsARequestThatIsNotChecked()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Authorized);
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser { Role = UserRole.Authorizer });

        var act = async () => await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only checked requests are visible to an authorizer.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestDoesNotExist()
    {
        await using var db = TestDbContext.Create();
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(new GetRequestByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        await using var db = TestDbContext.Create();
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser { IsAuthenticated = false });

        var act = async () => await handler.Handle(new GetRequestByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Authentication is required.");
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotRecognized()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new GetRequestByIdQueryHandler(db, new FakeCurrentUser { Role = (UserRole)99 });

        var act = async () => await handler.Handle(new GetRequestByIdQuery(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Fact]
    public void Validate_RejectsAnEmptyId()
    {
        var result = new GetRequestByIdQueryValidator().TestValidate(new GetRequestByIdQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(query => query.Id);
    }
}
