using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Domain.Entities;

namespace Project.Application.Tests.Support;

public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<WorkRequest> WorkRequests => Set<WorkRequest>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    public async Task<WorkRequest> AddRequestAsync(
        Guid createdByUserId,
        RequestStatus status = RequestStatus.Pending,
        DateTime? createdAt = null,
        string title = "Title",
        string description = "Description",
        Action<WorkRequest>? configure = null)
    {
        var user = await Users.FindAsync(createdByUserId);
        if (user is null)
        {
            user = TestData.User(id: createdByUserId);
            Users.Add(user);
        }

        var workRequest = TestData.Request(createdByUserId, status, createdAt, title, description);
        workRequest.CreatedByUser = user;
        configure?.Invoke(workRequest);
        WorkRequests.Add(workRequest);
        await SaveChangesAsync();
        return workRequest;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId);

        modelBuilder.Entity<WorkRequest>()
            .HasOne(request => request.CreatedByUser)
            .WithMany()
            .HasForeignKey(request => request.CreatedByUserId);
    }
}
