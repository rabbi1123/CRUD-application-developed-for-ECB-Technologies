using System.Security.Claims;
using Project.Application.Abstractions;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.API.Security;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public Guid UserId =>
        Guid.Parse(FindClaim(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedException("Authentication is required."));

    public string Email => FindClaim(ClaimTypes.Email) ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var role = FindClaim(ClaimTypes.Role) ?? throw new UnauthorizedException("Authentication is required.");
            return Enum.Parse<UserRole>(role);
        }
    }

    private string? FindClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
    }
}
