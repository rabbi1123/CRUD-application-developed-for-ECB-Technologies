using FluentValidation.TestHelper;
using Project.Application.Requests.Commands.RejectRequest;

namespace Project.Application.Tests.Requests;

public class RejectRequestCommandTests
{
    [Fact]
    public async Task Handle_RejectsAPendingRequest_AndStoresTrimmedCheckerRemarks()
    {
        await using var db = TestDbContext.Create();
        var checker = new FakeCurrentUser { Role = UserRole.Checker };
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new RejectRequestCommandHandler(db, checker);

        var result = await handler.Handle(
            new RejectRequestCommand(existing.Id, "  Needs more detail  "),
            CancellationToken.None);

        result.Status.Should().Be(nameof(RequestStatus.Rejected));
        result.CheckerRemarks.Should().Be("Needs more detail");
        var stored = await db.WorkRequests.SingleAsync();
        stored.Status.Should().Be(RequestStatus.Rejected);
        stored.CheckedByUserId.Should().Be(checker.UserId);
        stored.CheckedAt.Should().NotBeNull();
        stored.CheckerRemarks.Should().Be("Needs more detail");
        stored.AuthorizerRemarks.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RejectsACheckedRequest_AndStoresAuthorizerRemarks()
    {
        await using var db = TestDbContext.Create();
        var authorizer = new FakeCurrentUser { Role = UserRole.Authorizer };
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Checked, configure: request =>
        {
            request.CheckedByUserId = Guid.NewGuid();
            request.CheckedAt = DateTime.UtcNow.AddHours(-2);
            request.CheckerRemarks = "Looks complete";
        });
        var handler = new RejectRequestCommandHandler(db, authorizer);

        var result = await handler.Handle(new RejectRequestCommand(existing.Id, "   "), CancellationToken.None);

        result.Status.Should().Be(nameof(RequestStatus.Rejected));
        result.CheckerRemarks.Should().Be("Looks complete");
        result.AuthorizerRemarks.Should().BeNull();
        var stored = await db.WorkRequests.SingleAsync();
        stored.AuthorizedByUserId.Should().Be(authorizer.UserId);
        stored.AuthorizedAt.Should().NotBeNull();
        stored.AuthorizerRemarks.Should().BeNull();
        stored.CheckerRemarks.Should().Be("Looks complete");
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenACheckerRejectsARequestThatIsNotPending()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid(), RequestStatus.Checked);
        var handler = new RejectRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var act = async () => await handler.Handle(new RejectRequestCommand(existing.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only pending requests can be rejected by a checker.");
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenAnAuthorizerRejectsARequestThatIsNotChecked()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new RejectRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Authorizer });

        var act = async () => await handler.Handle(new RejectRequestCommand(existing.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only checked requests can be rejected by an authorizer.");
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenTheCallerIsAUser()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new RejectRequestCommandHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(new RejectRequestCommand(existing.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("You do not have access to this action.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestDoesNotExist()
    {
        await using var db = TestDbContext.Create();
        var handler = new RejectRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var act = async () => await handler.Handle(new RejectRequestCommand(Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        await using var db = TestDbContext.Create();
        var handler = new RejectRequestCommandHandler(db, new FakeCurrentUser { IsAuthenticated = false });

        var act = async () => await handler.Handle(new RejectRequestCommand(Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Authentication is required.");
    }

    [Fact]
    public void Validate_RejectsAnEmptyIdAndRemarksThatAreTooLong()
    {
        var result = new RejectRequestCommandValidator().TestValidate(
            new RejectRequestCommand(Guid.Empty, new string('r', 1001)));

        result.ShouldHaveValidationErrorFor(command => command.Id);
        result.ShouldHaveValidationErrorFor(command => command.Remarks);
    }

    [Fact]
    public void Validate_AcceptsAnIdWithoutRemarks()
    {
        var result = new RejectRequestCommandValidator().TestValidate(
            new RejectRequestCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
