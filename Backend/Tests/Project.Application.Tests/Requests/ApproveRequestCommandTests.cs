using FluentValidation.TestHelper;
using Project.Application.Requests.Commands.ApproveRequest;

namespace Project.Application.Tests.Requests;

public class ApproveRequestCommandTests
{
    [Fact]
    public async Task Handle_ChecksAPendingRequest_WhenTheCallerIsAChecker()
    {
        await using var db = TestDbContext.Create();
        var checker = new FakeCurrentUser { Role = UserRole.Checker };
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new ApproveRequestCommandHandler(db, checker);

        var result = await handler.Handle(new ApproveRequestCommand(existing.Id), CancellationToken.None);

        result.Status.Should().Be(nameof(RequestStatus.Checked));
        var stored = await db.WorkRequests.SingleAsync();
        stored.Status.Should().Be(RequestStatus.Checked);
        stored.CheckedByUserId.Should().Be(checker.UserId);
        stored.CheckedAt.Should().NotBeNull();
        stored.AuthorizedByUserId.Should().BeNull();
        stored.UpdatedAt.Should().Be(stored.CheckedAt);
    }

    [Fact]
    public async Task Handle_AuthorizesACheckedRequest_WhenTheCallerIsAnAuthorizer()
    {
        await using var db = TestDbContext.Create();
        var authorizer = new FakeCurrentUser { Role = UserRole.Authorizer };
        var checkedAt = DateTime.UtcNow.AddHours(-1);
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Checked, configure: request =>
        {
            request.CheckedByUserId = Guid.NewGuid();
            request.CheckedAt = checkedAt;
            request.CheckerRemarks = null;
        });
        var handler = new ApproveRequestCommandHandler(db, authorizer);

        var result = await handler.Handle(new ApproveRequestCommand(existing.Id), CancellationToken.None);

        result.Status.Should().Be(nameof(RequestStatus.Authorized));
        var stored = await db.WorkRequests.SingleAsync();
        stored.Status.Should().Be(RequestStatus.Authorized);
        stored.CheckedAt.Should().Be(checkedAt);
        stored.AuthorizedByUserId.Should().Be(authorizer.UserId);
        stored.AuthorizedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenACheckerApprovesARequestThatIsNotPending()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Checked);
        var handler = new ApproveRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var act = async () => await handler.Handle(new ApproveRequestCommand(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only pending requests can be checked.");
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenAnAuthorizerApprovesARequestThatIsNotChecked()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new ApproveRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Authorizer });

        var act = async () => await handler.Handle(new ApproveRequestCommand(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only checked requests can be authorized.");
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenTheCallerIsAUser()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new ApproveRequestCommandHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(new ApproveRequestCommand(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestDoesNotExist()
    {
        await using var db = TestDbContext.Create();
        var handler = new ApproveRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var act = async () => await handler.Handle(new ApproveRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        await using var db = TestDbContext.Create();
        var handler = new ApproveRequestCommandHandler(db, new FakeCurrentUser { IsAuthenticated = false });

        var act = async () => await handler.Handle(new ApproveRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Authentication is required.");
    }

    [Fact]
    public void Validate_RejectsAnEmptyId()
    {
        var result = new ApproveRequestCommandValidator().TestValidate(new ApproveRequestCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.Id);
    }

    [Fact]
    public void Validate_AcceptsAnId()
    {
        var result = new ApproveRequestCommandValidator().TestValidate(new ApproveRequestCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
