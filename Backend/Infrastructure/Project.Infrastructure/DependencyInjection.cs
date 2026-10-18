using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Security;

namespace Project.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetRequired(
            configuration,
            "ConnectionStrings:DefaultConnection",
            "ConnectionStrings__DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        var jwt = new JwtSettings
        {
            Key = GetRequired(configuration, "Jwt:Key", "Jwt__Key"),
            Issuer = GetRequired(configuration, "Jwt:Issuer", "Jwt__Issuer"),
            Audience = GetRequired(configuration, "Jwt:Audience", "Jwt__Audience"),
            AccessTokenMinutes = GetInt(configuration, "Jwt:AccessTokenMinutes", "Jwt__AccessTokenMinutes", 30),
            RefreshTokenDays = GetInt(configuration, "Jwt:RefreshTokenDays", "Jwt__RefreshTokenDays", 7)
        };

        services.Configure<JwtSettings>(options =>
        {
            options.Key = jwt.Key;
            options.Issuer = jwt.Issuer;
            options.Audience = jwt.Audience;
            options.AccessTokenMinutes = jwt.AccessTokenMinutes;
            options.RefreshTokenDays = jwt.RefreshTokenDays;
        });
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }

    private static string GetRequired(IConfiguration configuration, string key, string environmentVariable)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(environmentVariable);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"'{key}' is missing. Set {environmentVariable} in docker-compose.");
        }

        return value;
    }

    private static int GetInt(IConfiguration configuration, string key, string environmentVariable, int fallback)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(environmentVariable);
        }

        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
