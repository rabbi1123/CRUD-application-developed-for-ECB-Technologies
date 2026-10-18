using FluentValidation.TestHelper;
using Project.Application.Auth.Commands.RefreshToken;

namespace Project.Application.Tests.Auth;

public class RefreshTokenCommandTests
{
    private readonly FakeJwtTokenService _jwt = new();

    [Fact]
    public async Task Handle_RotatesToken_WhenStoredTokenIsActive()
    {
        await using var db = TestDbContext.Create();
        var user = TestData.User(role: UserRole.Authorizer);
        const string currentToken = "current-refresh-token";
        db.Users.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            User = user,
            UserId = user.Id,
            TokenHash = _jwt.HashRefreshToken(currentToken),
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        _jwt.EnqueueRefreshToken("next-refresh-token");
        var handler = new RefreshTokenCommandHandler(db, _jwt);

        var result = await handler.Handle(new RefreshTokenCommand(currentToken), CancellationToken.None);

        result.AccessToken.Should().Be($"access-{user.Id}");
        result.RefreshToken.Should().Be("next-refresh-token");
        result.AccessTokenExpiresAt.Should().Be(_jwt.AccessExpiresAt);
        result.Role.Should().Be(nameof(UserRole.Authorizer));

        var tokens = await db.RefreshTokens.OrderBy(token => token.CreatedAt).ToListAsync();
        tokens.Should().HaveCount(2);
        tokens[0].RevokedAt.Should().NotBeNull();
        tokens[1].UserId.Should().Be(user.Id);
        tokens[1].TokenHash.Should().Be(_jwt.HashRefreshToken("next-refresh-token"));
        tokens[1].ExpiresAt.Should().Be(_jwt.RefreshExpiresAt);
        tokens[1].RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTokenDoesNotExist()
    {
        await using var db = TestDbContext.Create();
        var handler = new RefreshTokenCommandHandler(db, _jwt);

        var act = async () => await handler.Handle(
            new RefreshTokenCommand("missing-token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token is invalid.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTokenIsRevoked()
    {
        await using var db = TestDbContext.Create();
        await SeedTokenAsync(db, "revoked-token", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddMinutes(-5));
        var handler = new RefreshTokenCommandHandler(db, _jwt);

        var act = async () => await handler.Handle(new RefreshTokenCommand("revoked-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token is invalid.");
        (await db.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTokenIsExpired()
    {
        await using var db = TestDbContext.Create();
        await SeedTokenAsync(db, "expired-token", DateTime.UtcNow.AddMinutes(-1), revokedAt: null);
        var handler = new RefreshTokenCommandHandler(db, _jwt);

        var act = async () => await handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token is invalid.");
        (await db.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Validate_RejectsAnEmptyToken()
    {
        var result = new RefreshTokenCommandValidator().TestValidate(new RefreshTokenCommand(""));

        result.ShouldHaveValidationErrorFor(command => command.RefreshToken);
    }

    [Fact]
    public void Validate_AcceptsAToken()
    {
        var result = new RefreshTokenCommandValidator().TestValidate(new RefreshTokenCommand("token"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private async Task SeedTokenAsync(TestDbContext db, string rawToken, DateTime expiresAt, DateTime? revokedAt)
    {
        var user = TestData.User();
        db.Users.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            User = user,
            UserId = user.Id,
            TokenHash = _jwt.HashRefreshToken(rawToken),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = revokedAt
        });
        await db.SaveChangesAsync();
    }
}
