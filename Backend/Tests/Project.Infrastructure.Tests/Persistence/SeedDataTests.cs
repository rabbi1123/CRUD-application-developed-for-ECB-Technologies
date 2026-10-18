using Microsoft.EntityFrameworkCore;
using Project.Domain.Enums;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Tests.Support;

namespace Project.Infrastructure.Tests.Persistence;

public class SeedDataTests
{
    [Fact]
    public async Task InitializeAsync_SeedsTheThreeDemoAccounts_WhenTheDatabaseIsEmpty()
    {
        await using var db = CreateContext();
        var hasher = new FakePasswordHasher();

        await SeedData.InitializeAsync(db, hasher);

        var users = await db.Users.OrderBy(user => user.Email).ToListAsync();
        users.Should().HaveCount(3);
        users.Select(user => (user.Email, user.FullName, user.Role, user.PasswordHash)).Should().Equal(
            ("authorizer@ecb.test", "Demo Authorizer", UserRole.Authorizer, hasher.Hash(SeedData.DemoPassword)),
            ("checker@ecb.test", "Demo Checker", UserRole.Checker, hasher.Hash(SeedData.DemoPassword)),
            ("user@ecb.test", "Demo User", UserRole.User, hasher.Hash(SeedData.DemoPassword)));
        users.Should().OnlyContain(user => user.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task InitializeAsync_DoesNotAddUsers_WhenAUserAlreadyExists()
    {
        await using var db = CreateContext();
        db.Users.Add(new Project.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            FullName = "Existing",
            Email = "existing@ecb.test",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await SeedData.InitializeAsync(db, new ThrowingPasswordHasher());

        (await db.Users.CountAsync()).Should().Be(1);
        (await db.Users.AnyAsync(user => user.Email == "user@ecb.test")).Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        await using var db = CreateContext();
        var hasher = new FakePasswordHasher();

        await SeedData.InitializeAsync(db, hasher);
        await SeedData.InitializeAsync(db, new ThrowingPasswordHasher());

        (await db.Users.CountAsync()).Should().Be(3);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
