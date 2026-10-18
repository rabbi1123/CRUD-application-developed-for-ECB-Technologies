using FluentValidation.TestHelper;
using Project.Application.Requests.Commands.DeleteRequest;

namespace Project.Application.Tests.Requests;

public class DeleteRequestCommandTests
{
    [Theory]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Handle_DeletesARequest_WhenTheOwnerCanStillRemoveIt(RequestStatus status)
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        var existing = await db.AddRequestAsync(currentUser.UserId, status);
        var handler = new DeleteRequestCommandHandler(db, currentUser);

        await handler.Handle(new DeleteRequestCommand(existing.Id), CancellationToken.None);

        (await db.WorkRequests.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(RequestStatus.Checked)]
    [InlineData(RequestStatus.Authorized)]
    public async Task Handle_ThrowsConflict_WhenTheRequestCanNoLongerBeDeleted(RequestStatus status)
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        var existing = await db.AddRequestAsync(currentUser.UserId, status);
        var handler = new DeleteRequestCommandHandler(db, currentUser);

        var act = async () => await handler.Handle(new DeleteRequestCommand(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only pending or rejected requests can be deleted.");
        (await db.WorkRequests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestBelongsToSomeoneElse()
    {
        await using var db = TestDbContext.Create();
        var existing = await db.AddRequestAsync(Guid.NewGuid());
        var handler = new DeleteRequestCommandHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(new DeleteRequestCommand(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenTheRequestDoesNotExist()
    {
        await using var db = TestDbContext.Create();
        var handler = new DeleteRequestCommandHandler(db, new FakeCurrentUser());

        var act = async () => await handler.Handle(new DeleteRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Request was not found.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        await using var db = TestDbContext.Create();
        var handler = new DeleteRequestCommandHandler(db, new FakeCurrentUser { IsAuthenticated = false });

        var act = async () => await handler.Handle(new DeleteRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Authentication is required.");
    }

    [Fact]
    public void Validate_RejectsAnEmptyId()
    {
        var result = new DeleteRequestCommandValidator().TestValidate(new DeleteRequestCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.Id);
    }

    [Fact]
    public void Validate_AcceptsAnId()
    {
        var result = new DeleteRequestCommandValidator().TestValidate(new DeleteRequestCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
