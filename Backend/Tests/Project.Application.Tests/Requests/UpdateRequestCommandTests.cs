using FluentValidation.TestHelper;
using Project.Application.Requests.Commands.UpdateRequest;

namespace Project.Application.Tests.Requests;

public class UpdateRequestCommandTests
{
    [Fact]
    public async Task Handle_UpdatesAPendingRequest_OwnedByTheCurrentUser()
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        var existing = await db.AddRequestAsync(currentUser.UserId);
        var handler = new UpdateRequestCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new UpdateRequestCommand(existing.Id, "  Updated title  ", "  Updated description  "),
            CancellationToken.None);

        result.Title.Should().Be("Updated title");
        result.Description.Should().Be("Updated description");
        result.Status.Should().Be(nameof(RequestStatus.Pending));

        var stored = await db.WorkRequests.SingleAsync();
        stored.Title.Should().Be("Updated title");
        stored.Status.Should().Be(RequestStatus.Pending);
        stored.UpdatedAt.Should().BeAfter(existing.CreatedAt);
    }

    [Fact]
    public async Task Handle_ReopensARejectedRequest_AndClearsReviewFields()
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        var existing = await db.AddRequestAsync(currentUser.UserId, RequestStatus.Rejected, configure: request =>
        {
            request.CheckedByUserId = Guid.NewGuid();
            request.CheckedAt = DateTime.UtcNow.AddDays(-1);
            request.CheckerRemarks = "Needs detail";
            request.AuthorizedByUserId = Guid.NewGuid();
            request.AuthorizedAt = DateTime.UtcNow.AddHours(-2);
            request.AuthorizerRemarks = "Not ready";
        });
        var handler = new UpdateRequestCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new UpdateRequestCommand(existing.Id, "Revised title", "Revised description"),
            CancellationToken.None);

        result.Status.Should().Be(nameof(RequestStatus.Pending));
        result.CheckerRemarks.Should().BeNull();
        result.AuthorizerRemarks.Should().BeNull();

        var stored = await db.WorkRequests.SingleAsync();
        stored.Status.Should().Be(RequestStatus.Pending);
        stored.CheckedByUserId.Should().BeNull();
        stored.CheckedAt.Should().BeNull();
        stored.CheckerRemarks.Should().BeNull();
        stored.AuthorizedByUserId.Should().BeNull();
        stored.AuthorizedAt.Should().BeNull();
        stored.AuthorizerRemarks.Should().BeNull();
    }

    [Theory]
    [InlineData(RequestStatus.Checked)]
    [InlineData(RequestStatus.Authorized)]
    public async Task Handle_ThrowsConflict_WhenTheRequestCanNoLongerBeEdited(RequestStatus status)
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        var existing = await db.AddRequestAsync(currentUser.UserId, status);
        var handler = new UpdateRequestCommandHandler(db, currentUser);

        var act = async () => await handler.Handle(
            new UpdateRequestCommand(existing.Id, "Title", "Description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only pending or rejected requests can be updated.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestBelongsToSomeoneElse()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new UpdateRequestCommandHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(
            new UpdateRequestCommand(existing.Id, "Title", "Description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestDoesNotExist()
    {
        await using var db = TestDbContext.Create();
        var handler = new UpdateRequestCommandHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(
            new UpdateRequestCommand(Guid.NewGuid(), "Title", "Description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotUser()
    {
        await using var db = TestDbContext.Create();
        var handler = new UpdateRequestCommandHandler(db, new FakeCurrentUser { Role = UserRole.Checker });

        var act = async () => await handler.Handle(
            new UpdateRequestCommand(Guid.NewGuid(), "Title", "Description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Fact]
    public void Validate_RejectsInvalidCommands()
    {
        var result = new UpdateRequestCommandValidator().TestValidate(
            new UpdateRequestCommand(Guid.Empty, "", new string('d', 2001)));

        result.ShouldHaveValidationErrorFor(command => command.Id);
        result.ShouldHaveValidationErrorFor(command => command.Title);
        result.ShouldHaveValidationErrorFor(command => command.Description);
    }

    [Fact]
    public void Validate_AcceptsAValidCommand()
    {
        var result = new UpdateRequestCommandValidator().TestValidate(
            new UpdateRequestCommand(Guid.NewGuid(), "Title", "Description"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
