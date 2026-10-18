using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
    }
}
