using Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class MedicalHistoriesConfiguration: BaseEntityConfiguration<MedicalHistory>
{
    public override void Configure(EntityTypeBuilder<MedicalHistory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Nota).IsRequired();
        builder.Property(x => x.Fecha).IsRequired();
         
    }
}