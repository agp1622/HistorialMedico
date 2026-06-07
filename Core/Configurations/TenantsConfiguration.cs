using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class TenantsConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(100);

        // Slugs back subdomain routing and tenant lookup, so they must be unique.
        builder.HasIndex(e => e.Slug)
            .IsUnique();

        builder.Property(e => e.BillingStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Plan)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);
    }
}
