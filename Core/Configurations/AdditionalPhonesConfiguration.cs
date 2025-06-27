using Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class AdditionalPhonesConfiguration: BaseEntityConfiguration<AdditionalPhone>
{
    public override void Configure(EntityTypeBuilder<AdditionalPhone> builder)
    {
        base.Configure(builder);

       builder.Property(x => x.Number).IsRequired();
    }
}