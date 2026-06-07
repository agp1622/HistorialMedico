using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public abstract class BaseEntityConfiguration<T>: IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.TenantId)
            .IsRequired();

        // Every tenant-scoped query filters by TenantId (see global query filters in
        // HistorialDbContext), so an index here is essential for performance.
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        builder.Property(e => e.UpdatedAt);
    }
}