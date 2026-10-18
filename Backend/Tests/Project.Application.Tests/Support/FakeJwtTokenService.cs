using Project.Application.Abstractions;
using Project.Domain.Entities;

namespace Project.Application.Tests.Support;

public sealed class FakeJwtTokenService : IJwtTokenService
{
    private readonly Queue<string> _refreshTokens = new();

    public DateTime AccessExpiresAt { get; } = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    public DateTime RefreshExpiresAt { get; } = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    public void EnqueueRefreshToken(string token) => _refreshTokens.Enqueue(token);

    public AccessToken CreateAccessToken(User user) => new($"access-{user.Id}", AccessExpiresAt);

    public string GenerateRefreshToken() =>
        _refreshTokens.Count > 0 ? _refreshTokens.Dequeue() : $"refresh-{Guid.NewGuid():N}";

    public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";

    public DateTime GetRefreshTokenExpiry() => RefreshExpiresAt;
}
