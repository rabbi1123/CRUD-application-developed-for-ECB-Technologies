using Project.Domain.Entities;

namespace Project.Application.Abstractions;

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    DateTime GetRefreshTokenExpiry();
}

public record AccessToken(string Token, DateTime ExpiresAt);
