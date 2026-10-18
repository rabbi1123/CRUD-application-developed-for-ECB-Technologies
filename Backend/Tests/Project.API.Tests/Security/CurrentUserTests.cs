using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Project.API.Security;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.API.Tests.Security;

public class CurrentUserTests
{
    [Fact]
    public void IsAuthenticated_IsFalse_WhenThereIsNoHttpContext()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());

        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.Email.Should().BeEmpty();
        var userId = () => currentUser.UserId;
        var role = () => currentUser.Role;
        userId.Should().Throw<UnauthorizedException>().WithMessage("Authentication is required.");
        role.Should().Throw<UnauthorizedException>().WithMessage("Authentication is required.");
    }

    [Fact]
    public void Claims_AreReadFromTheAuthenticatedPrincipal()
    {
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(authenticated: true, userId, "checker@ecb.test", UserRole.Checker);

        currentUser.IsAuthenticated.Should().BeTrue();
        currentUser.UserId.Should().Be(userId);
        currentUser.Email.Should().Be("checker@ecb.test");
        currentUser.Role.Should().Be(UserRole.Checker);
    }

    [Fact]
    public void IsAuthenticated_IsFalse_WhenTheIdentityHasNoAuthenticationType()
    {
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(authenticated: false, userId, "user@ecb.test", UserRole.User);

        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.UserId.Should().Be(userId);
        currentUser.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void Email_IsEmpty_WhenTheEmailClaimIsMissing()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                    "Test"))
            }
        };

        new CurrentUser(accessor).Email.Should().BeEmpty();
    }

    private static CurrentUser CreateCurrentUser(bool authenticated, Guid userId, string email, UserRole role)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            authenticated ? "Test" : null);

        return new CurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        });
    }
}
