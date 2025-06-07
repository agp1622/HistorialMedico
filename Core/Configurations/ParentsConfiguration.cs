using Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class ParentsConfiguration: BaseEntityConfiguration<Parent>
{
    public override void Configure(EntityTypeBuilder<Parent> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Nombre).IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.Apellido).IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.Telefono).IsRequired();
        builder.Property(x => x.Email);
    }
}