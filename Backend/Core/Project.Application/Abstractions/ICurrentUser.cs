using Project.Domain.Enums;

namespace Project.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    string Email { get; }

    UserRole Role { get; }
}
