using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;
public class RolesConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Configure custom properties only
        builder.Property(r => r.Description)
            .HasMaxLength(500);
            
        builder.Property(r => r.CreatedAt)
            .IsRequired();
    }
}