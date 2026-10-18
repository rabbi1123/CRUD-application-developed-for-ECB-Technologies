using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.Infrastructure.Persistence.Configurations;

public class WorkRequestConfiguration : IEntityTypeConfiguration<WorkRequest>
{
    public void Configure(EntityTypeBuilder<WorkRequest> builder)
    {
        builder.ToTable("Requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Title).HasMaxLength(200).IsRequired();
        builder.Property(request => request.Description).HasMaxLength(2000).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(request => request.CheckerRemarks).HasMaxLength(1000);
        builder.Property(request => request.AuthorizerRemarks).HasMaxLength(1000);
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.CreatedByUserId);

        builder.HasOne(request => request.CreatedByUser)
            .WithMany()
            .HasForeignKey(request => request.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
