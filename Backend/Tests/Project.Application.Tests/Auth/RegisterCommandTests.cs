using FluentValidation.TestHelper;
using Project.Application.Auth.Commands.Register;

namespace Project.Application.Tests.Auth;

public class RegisterCommandTests
{
    private readonly FakePasswordHasher _passwordHasher = new();

    [Fact]
    public async Task Handle_CreatesUser_WhenEmailIsAvailable()
    {
        await using var db = TestDbContext.Create();
        var handler = new RegisterCommandHandler(db, _passwordHasher);

        var result = await handler.Handle(
            new RegisterCommand("  Ada Lovelace  ", "  Ada@ECB.Test  ", "Password123!"),
            CancellationToken.None);

        result.FullName.Should().Be("Ada Lovelace");
        result.Email.Should().Be("ada@ecb.test");
        result.Role.Should().Be(nameof(UserRole.User));

        var user = await db.Users.SingleAsync();
        user.Id.Should().Be(result.Id);
        user.PasswordHash.Should().Be(_passwordHasher.Hash("Password123!"));
        user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenEmailAlreadyExists()
    {
        await using var db = TestDbContext.Create();
        db.Users.Add(TestData.User(email: "ada@ecb.test"));
        await db.SaveChangesAsync();
        var handler = new RegisterCommandHandler(db, _passwordHasher);

        var act = async () => await handler.Handle(
            new RegisterCommand("Ada", " ADA@ecb.test ", "Password123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("An account with this email already exists.");
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Validate_RejectsInvalidCommands()
    {
        var result = new RegisterCommandValidator().TestValidate(
            new RegisterCommand("", "not-an-email", "short"));

        result.ShouldHaveValidationErrorFor(command => command.FullName);
        result.ShouldHaveValidationErrorFor(command => command.Email);
        result.ShouldHaveValidationErrorFor(command => command.Password);
    }

    [Fact]
    public void Validate_RejectsValuesThatAreTooLong()
    {
        var result = new RegisterCommandValidator().TestValidate(
            new RegisterCommand(new string('a', 201), new string('a', 248) + "@ecb.test", new string('p', 101)));

        result.ShouldHaveValidationErrorFor(command => command.FullName);
        result.ShouldHaveValidationErrorFor(command => command.Email);
        result.ShouldHaveValidationErrorFor(command => command.Password);
    }

    [Fact]
    public void Validate_AcceptsAValidCommand()
    {
        var result = new RegisterCommandValidator().TestValidate(
            new RegisterCommand("Ada Lovelace", "ada@ecb.test", "Password123!"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
