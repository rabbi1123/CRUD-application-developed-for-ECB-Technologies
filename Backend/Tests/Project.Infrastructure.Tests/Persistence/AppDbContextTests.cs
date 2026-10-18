using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence;

namespace Project.Infrastructure.Tests.Persistence;

public class AppDbContextTests
{
    [Fact]
    public void Sets_ExposeTheApplicationCollections()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);

        context.Users.Should().NotBeNull();
        context.WorkRequests.Should().NotBeNull();
        context.RefreshTokens.Should().NotBeNull();
        ((IApplicationDbContext)context).Should().BeSameAs(context);
    }

    [Fact]
    public void CreateDbContext_BuildsASqlServerContext()
    {
        using var context = new AppDbContextFactory().CreateDbContext([]);

        context.Should().BeOfType<AppDbContext>();
        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }
}
