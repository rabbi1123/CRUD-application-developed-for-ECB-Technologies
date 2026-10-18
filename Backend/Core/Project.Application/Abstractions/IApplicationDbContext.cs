using Microsoft.EntityFrameworkCore;
using Project.Domain.Entities;

namespace Project.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<WorkRequest> WorkRequests { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
