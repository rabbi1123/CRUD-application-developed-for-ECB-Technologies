using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Project.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string DesignTimeConnectionString =
		"Server=sqlserver,1433;Database=EcbDb;User Id=sa;Password=Ecb_Sql_Passw0rd!;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
