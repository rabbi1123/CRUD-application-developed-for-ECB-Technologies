using Project.Application.Abstractions;
using Project.Domain.Enums;

namespace Project.Application.Tests.Support;

public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;

    public Guid UserId { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = "user@ecb.test";

    public UserRole Role { get; set; } = UserRole.User;
}
