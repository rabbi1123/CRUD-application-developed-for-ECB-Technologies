using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.Application.Tests.Support;

public static class TestData
{
    public static User User(
        Guid? id = null,
        string fullName = "Demo User",
        string email = "user@ecb.test",
        string passwordHash = "hash",
        UserRole role = UserRole.User)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static WorkRequest Request(
        Guid createdByUserId,
        RequestStatus status = RequestStatus.Pending,
        DateTime? createdAt = null,
        string title = "Title",
        string description = "Description")
    {
        var created = createdAt ?? DateTime.UtcNow;
        return new WorkRequest
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Status = status,
            CreatedByUserId = createdByUserId,
            CreatedAt = created,
            UpdatedAt = created
        };
    }
}
