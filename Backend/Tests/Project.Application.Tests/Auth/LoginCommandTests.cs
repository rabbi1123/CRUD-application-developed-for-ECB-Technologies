using FluentValidation.TestHelper;
using Project.Application.Auth.Commands.Login;

namespace Project.Application.Tests.Auth;

public class LoginCommandTests
{
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeJwtTokenService _jwt = new();

    [Fact]
    public async Task Handle_IssuesTokens_WhenCredentialsMatch()
    {
        await using var db = TestDbContext.Create();
        var user = TestData.User(
            email: "user@ecb.test",
            passwordHash: _passwordHasher.Hash("Password123!"),
            role: UserRole.Checker);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        _jwt.EnqueueRefreshToken("issued-refresh-token");
        var handler = new LoginCommandHandler(db, _passwordHasher, _jwt);

        var result = await handler.Handle(
            new LoginCommand("  User@ECB.Test  ", "Password123!"),
            CancellationToken.None);

        result.AccessToken.Should().Be($"access-{user.Id}");
        result.RefreshToken.Should().Be("issued-refresh-token");
        result.AccessTokenExpiresAt.Should().Be(_jwt.AccessExpiresAt);
        result.Role.Should().Be(nameof(UserRole.Checker));

        var stored = await db.RefreshTokens.SingleAsync();
        stored.UserId.Should().Be(user.Id);
        stored.TokenHash.Should().Be(_jwt.HashRefreshToken("issued-refresh-token"));
        stored.ExpiresAt.Should().Be(_jwt.RefreshExpiresAt);
        stored.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenEmailIsUnknown()
    {
        await using var db = TestDbContext.Create();
        var handler = new LoginCommandHandler(db, _passwordHasher, _jwt);

        var act = async () => await handler.Handle(
            new LoginCommand("missing@ecb.test", "Password123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
        (await db.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenPasswordDoesNotMatch()
    {
        await using var db = TestDbContext.Create();
        db.Users.Add(TestData.User(passwordHash: _passwordHasher.Hash("Password123!")));
        await db.SaveChangesAsync();
        var handler = new LoginCommandHandler(db, _passwordHasher, _jwt);

        var act = async () => await handler.Handle(
            new LoginCommand("user@ecb.test", "wrong-password"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
        (await db.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Validate_RejectsInvalidCommands()
    {
        var result = new LoginCommandValidator().TestValidate(new LoginCommand("not-an-email", ""));

        result.ShouldHaveValidationErrorFor(command => command.Email);
        result.ShouldHaveValidationErrorFor(command => command.Password);
    }

    [Fact]
    public void Validate_AcceptsAValidCommand()
    {
        var result = new LoginCommandValidator().TestValidate(
            new LoginCommand("user@ecb.test", "Password123!"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
