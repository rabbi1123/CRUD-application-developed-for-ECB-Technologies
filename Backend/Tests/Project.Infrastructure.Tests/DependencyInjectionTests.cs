using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Security;
using Project.Infrastructure.Tests.Support;

namespace Project.Infrastructure.Tests;

public class DependencyInjectionTests
{
    private static readonly string[] EnvironmentKeys =
    [
        "ConnectionStrings__DefaultConnection",
        "Jwt__Key",
        "Jwt__Issuer",
        "Jwt__Audience",
        "Jwt__AccessTokenMinutes",
        "Jwt__RefreshTokenDays"
    ];

    [Fact]
    public void AddInfrastructure_RegistersPersistenceAndSecurity_FromConfiguration()
    {
        using var _ = new EnvironmentVariableScope(EnvironmentKeys);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=FromConfig;User Id=sa;Password=secret;TrustServerCertificate=True",
            ["Jwt:Key"] = new string('k', 40),
            ["Jwt:Issuer"] = "from-config",
            ["Jwt:Audience"] = "from-config-api",
            ["Jwt:AccessTokenMinutes"] = "12",
            ["Jwt:RefreshTokenDays"] = "9"
        });

        using var provider = BuildProvider(configuration);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        scope.ServiceProvider.GetRequiredService<IApplicationDbContext>().Should().BeSameAs(context);
        context.Database.GetConnectionString().Should().Be("Server=localhost;Database=FromConfig;User Id=sa;Password=secret;TrustServerCertificate=True");
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Should().BeOfType<BcryptPasswordHasher>();
        scope.ServiceProvider.GetRequiredService<IJwtTokenService>().Should().BeOfType<JwtTokenService>();
        settings.Key.Should().HaveLength(40);
        settings.Issuer.Should().Be("from-config");
        settings.Audience.Should().Be("from-config-api");
        settings.AccessTokenMinutes.Should().Be(12);
        settings.RefreshTokenDays.Should().Be(9);
    }

    [Fact]
    public void AddInfrastructure_UsesEnvironmentVariables_WhenConfigurationValuesAreMissing()
    {
        using var scopeVariables = new EnvironmentVariableScope(EnvironmentKeys);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=localhost;Database=FromEnv;User Id=sa;Password=secret;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__Key", new string('e', 36));
        Environment.SetEnvironmentVariable("Jwt__Issuer", "from-env");
        Environment.SetEnvironmentVariable("Jwt__Audience", "from-env-api");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "not-a-number");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "");

        using var provider = BuildProvider(BuildConfiguration(new Dictionary<string, string?>()));
        using var scope = provider.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()
            .Should().Be("Server=localhost;Database=FromEnv;User Id=sa;Password=secret;TrustServerCertificate=True");
        settings.Issuer.Should().Be("from-env");
        settings.Audience.Should().Be("from-env-api");
        settings.AccessTokenMinutes.Should().Be(30);
        settings.RefreshTokenDays.Should().Be(7);
    }

    [Fact]
    public void AddInfrastructure_PrefersConfiguration_OverEnvironmentVariables()
    {
        using var scopeVariables = new EnvironmentVariableScope(EnvironmentKeys);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=localhost;Database=FromEnv;User Id=sa;Password=secret;TrustServerCertificate=True");
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=FromConfig;User Id=sa;Password=secret;TrustServerCertificate=True",
            ["Jwt:Key"] = new string('k', 32),
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience"
        });

        using var provider = BuildProvider(configuration);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()
            .Should().Contain("Database=FromConfig");
    }

    [Fact]
    public void AddInfrastructure_Throws_WhenTheConnectionStringIsMissing()
    {
        using var _ = new EnvironmentVariableScope(EnvironmentKeys);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('k', 32),
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience"
        });

        var act = () => BuildProvider(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'ConnectionStrings:DefaultConnection' is missing*ConnectionStrings__DefaultConnection*");
    }

    [Fact]
    public void AddInfrastructure_Throws_WhenAJwtSettingIsMissing()
    {
        using var _ = new EnvironmentVariableScope(EnvironmentKeys);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=EcbTests;User Id=sa;Password=secret;TrustServerCertificate=True",
            ["Jwt:Key"] = new string('k', 32),
            ["Jwt:Audience"] = "audience"
        });

        var act = () => BuildProvider(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'Jwt:Issuer' is missing*Jwt__Issuer*");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
