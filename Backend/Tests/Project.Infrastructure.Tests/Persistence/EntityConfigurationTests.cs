using Microsoft.EntityFrameworkCore;
using Project.Domain.Entities;
using Project.Infrastructure.Persistence;

namespace Project.Infrastructure.Tests.Persistence;

public class EntityConfigurationTests : IDisposable
{
    private readonly AppDbContext _context = new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer("Server=localhost;Database=EcbModelTests;User Id=sa;Password=ignored;TrustServerCertificate=True")
        .Options);

    [Fact]
    public void User_IsMappedToUsers_WithAUniqueEmail()
    {
        var entity = _context.Model.FindEntityType(typeof(User))!;

        entity.GetTableName().Should().Be("Users");
        entity.FindProperty(nameof(User.FullName))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(User.Email))!.GetMaxLength().Should().Be(256);
        entity.FindProperty(nameof(User.PasswordHash))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(User.Role))!.GetMaxLength().Should().Be(32);
        entity.FindProperty(nameof(User.FullName))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(User.Role))!.GetProviderClrType().Should().Be(typeof(string));

        var emailIndex = entity.GetIndexes().Single(index => index.Properties.Single().Name == nameof(User.Email));
        emailIndex.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void WorkRequest_IsMappedToRequests_WithARestrictDeleteToTheCreator()
    {
        var entity = _context.Model.FindEntityType(typeof(WorkRequest))!;

        entity.GetTableName().Should().Be("Requests");
        entity.FindProperty(nameof(WorkRequest.Title))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(WorkRequest.Description))!.GetMaxLength().Should().Be(2000);
        entity.FindProperty(nameof(WorkRequest.CheckerRemarks))!.GetMaxLength().Should().Be(1000);
        entity.FindProperty(nameof(WorkRequest.AuthorizerRemarks))!.GetMaxLength().Should().Be(1000);
        entity.FindProperty(nameof(WorkRequest.Status))!.GetProviderClrType().Should().Be(typeof(string));
        entity.GetIndexes().Select(index => index.Properties.Single().Name)
            .Should().BeEquivalentTo(nameof(WorkRequest.Status), nameof(WorkRequest.CreatedByUserId));

        var foreignKey = entity.GetForeignKeys().Single(key =>
            key.Properties.Any(property => property.Name == nameof(WorkRequest.CreatedByUserId)));
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void RefreshToken_HasAUniqueHash_AndCascadesWithItsUser()
    {
        var entity = _context.Model.FindEntityType(typeof(RefreshToken))!;

        entity.GetTableName().Should().Be("RefreshTokens");
        entity.FindProperty(nameof(RefreshToken.TokenHash))!.GetMaxLength().Should().Be(128);
        entity.FindProperty(nameof(RefreshToken.TokenHash))!.IsNullable.Should().BeFalse();

        var hashIndex = entity.GetIndexes().Single(index => index.Properties.Single().Name == nameof(RefreshToken.TokenHash));
        hashIndex.IsUnique.Should().BeTrue();

        var foreignKey = entity.GetForeignKeys().Single();
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        foreignKey.PrincipalEntityType.ClrType.Should().Be(typeof(User));
    }

    public void Dispose() => _context.Dispose();
}
