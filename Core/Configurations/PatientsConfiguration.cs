using Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class PatientsConfiguration: BaseEntityConfiguration<Patient>
{
    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.BirthDate).IsRequired();

        builder.Property(x => x.FirstLastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SecondLastName)
            .HasMaxLength(100);
        
        builder.Property(x => x.MiddleName)
            .HasMaxLength(100);
    }
}