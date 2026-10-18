using Project.Application.Abstractions;
using Project.Application.Exceptions;
using Project.Domain.Enums;

namespace Project.Application.Common;

internal static class CurrentUserGuard
{
    public static void RequireRole(ICurrentUser currentUser, UserRole role)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        if (currentUser.Role != role)
        {
            throw new ForbiddenException("You do not have access to this action.");
        }
    }
}
