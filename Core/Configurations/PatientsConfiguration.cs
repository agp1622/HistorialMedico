using System.Xml;
using Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class PatientsConfiguration: BaseEntityConfiguration<Patient>
{
    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.NumExpediente).IsRequired();
        builder.Property(x => x.Diagnostico).IsRequired();
        builder.Property(x => x.Nombre).IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.Apellido).IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.EdadEnPrimeraConsulta);
        builder.Property(x => x.Sexo).IsRequired();
        builder.Property(x => x.ReferidoPor);
        builder.Property(x => x.FechaConsulta);
        builder.Property(x => x.SeguroMedico).IsRequired();
        builder.Property(x => x.Madre);
        builder.Property(x => x.Padre);
        builder.Property(x => x.Gestacion);
        builder.Property(x => x.PesoAlNacer);
    }
}