using FluentValidation.TestHelper;
using Project.Application.Requests.Commands.CreateRequest;

namespace Project.Application.Tests.Requests;

public class CreateRequestCommandTests
{
    [Fact]
    public async Task Handle_CreatesAPendingRequest_ForTheCurrentUser()
    {
        await using var db = TestDbContext.Create();
        var currentUser = new FakeCurrentUser();
        db.Users.Add(TestData.User(id: currentUser.UserId, email: currentUser.Email));
        await db.SaveChangesAsync();
        var handler = new CreateRequestCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new CreateRequestCommand("  New title  ", "  New description  "),
            CancellationToken.None);

        result.Title.Should().Be("New title");
        result.Description.Should().Be("New description");
        result.Status.Should().Be(nameof(RequestStatus.Pending));
        result.CreatedByUserId.Should().Be(currentUser.UserId);
        result.CheckedAt.Should().BeNull();
        result.AuthorizedAt.Should().BeNull();

        var stored = await db.WorkRequests.SingleAsync();
        stored.Id.Should().Be(result.Id);
        stored.CreatedAt.Should().Be(stored.UpdatedAt);
        stored.Status.Should().Be(RequestStatus.Pending);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTheCallerIsNotAuthenticated()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateRequestCommandHandler(db, new FakeCurrentUser { IsAuthenticated = false });

        var act = async () => await handler.Handle(
            new CreateRequestCommand("Title", "Description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Authentication is required.");
        (await db.WorkRequests.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(UserRole.Checker)]
    [InlineData(UserRole.Authorizer)]
    public async Task Handle_ThrowsForbidden_WhenTheRoleIsNotUser(UserRole role)
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateRequestCommandHandler(db, new FakeCurrentUser { Role = role });

        var act = async () => await handler.Handle(
            new CreateRequestCommand("Title", "Description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this action.");
    }

    [Fact]
    public void Validate_RejectsInvalidCommands()
    {
        var result = new CreateRequestCommandValidator().TestValidate(new CreateRequestCommand("", ""));

        result.ShouldHaveValidationErrorFor(command => command.Title);
        result.ShouldHaveValidationErrorFor(command => command.Description);
    }

    [Fact]
    public void Validate_RejectsValuesThatAreTooLong()
    {
        var result = new CreateRequestCommandValidator().TestValidate(
            new CreateRequestCommand(new string('t', 201), new string('d', 2001)));

        result.ShouldHaveValidationErrorFor(command => command.Title);
        result.ShouldHaveValidationErrorFor(command => command.Description);
    }

    [Fact]
    public void Validate_AcceptsAValidCommand()
    {
        var result = new CreateRequestCommandValidator().TestValidate(
            new CreateRequestCommand("Title", "Description"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
