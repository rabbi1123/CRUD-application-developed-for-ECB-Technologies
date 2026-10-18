using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.Infrastructure.Security;

namespace Project.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private const string SigningKey = "12345678901234567890123456789012";

    [Fact]
    public void CreateAccessToken_ReturnsASignedTokenWithTheUserClaims()
    {
        var service = CreateService();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@ecb.test",
            Role = UserRole.Checker
        };

        var accessToken = service.CreateAccessToken(user);

        accessToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        jwt.Issuer.Should().Be("ecb-tests");
        jwt.Audiences.Should().Contain("ecb-tests-api");
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Email && claim.Value == user.Email);
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == nameof(UserRole.Checker));

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            accessToken.Token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "ecb-tests",
                ValidAudience = "ecb-tests-api",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            },
            out _);

        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short-key")]
    public void CreateAccessToken_Throws_WhenTheSigningKeyIsShorterThan32Characters(string key)
    {
        var service = CreateService(key);
        var user = new User { Id = Guid.NewGuid(), Email = "user@ecb.test", Role = UserRole.User };

        var act = () => service.CreateAccessToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Jwt:Key must be at least 32 characters.");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsADistinctBase64Value()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        first.Should().NotBe(second);
        Convert.FromBase64String(first).Should().HaveCount(64);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministicUppercaseHex()
    {
        var service = CreateService();

        var hash = service.HashRefreshToken("refresh-token");

        hash.Should().Be(service.HashRefreshToken("refresh-token"));
        hash.Should().MatchRegex("^[0-9A-F]{64}$");
        hash.Should().NotBe(service.HashRefreshToken("other-token"));
    }

    [Fact]
    public void GetRefreshTokenExpiry_UsesTheConfiguredLifetime()
    {
        var service = CreateService(refreshTokenDays: 3);

        service.GetRefreshTokenExpiry().Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SectionName_IsJwt()
    {
        JwtSettings.SectionName.Should().Be("Jwt");
    }

    private static JwtTokenService CreateService(string? key = null, int refreshTokenDays = 7)
    {
        return new JwtTokenService(Options.Create(new JwtSettings
        {
            Key = key ?? SigningKey,
            Issuer = "ecb-tests",
            Audience = "ecb-tests-api",
            AccessTokenMinutes = 15,
            RefreshTokenDays = refreshTokenDays
        }));
    }
}
