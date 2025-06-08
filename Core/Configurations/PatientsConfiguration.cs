using System.Xml;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Configurations;

public class PatientsConfiguration: BaseEntityConfiguration<Patient>
{
    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.NumeroExpediente).IsRequired();
        builder.Property(x => x.Diagnostico).IsRequired();
        builder.Property(x => x.Nombre).IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.Edad);
        builder.Property(x => x.Sexo).IsRequired();
        builder.Property(x => x.ReferidoPor);
        builder.Property(x => x.FechaConsulta);
        builder.Property(x => x.SeguroMedico).IsRequired();
        builder.Property(x => x.Madre);
        builder.Property(x => x.Padre);
        builder.Property(x => x.Gestacion);
        builder.Property(x => x.PesoAlNacer);
        
        builder.HasMany(p => p.Historial)
            .WithOne(h => h.Patient)
            .HasForeignKey(h => h.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);    }
}