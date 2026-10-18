using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.Infrastructure.Persistence;

public static class SeedData
{
    public const string DemoPassword = "Password123!";

    public static async Task InitializeAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var createdAt = DateTime.UtcNow;
        var passwordHash = passwordHasher.Hash(DemoPassword);

        db.Users.AddRange(
            CreateUser("Demo User", "user@ecb.test", UserRole.User, passwordHash, createdAt),
            CreateUser("Demo Checker", "checker@ecb.test", UserRole.Checker, passwordHash, createdAt),
            CreateUser("Demo Authorizer", "authorizer@ecb.test", UserRole.Authorizer, passwordHash, createdAt));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(string fullName, string email, UserRole role, string passwordHash, DateTime createdAt)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = createdAt
        };
    }
}
