using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Entities;

public class Patient : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    // Información Personal
    public string NumExpediente { get; set; }
    public string Diagnostico { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string EdadEnPrimeraConsulta { get; set; }
    public Sexo Sexo { get; set; }
    public string ReferidoPor { get; set; }
    public DateTime FechaConsulta { get; set; }
    public string SeguroMedico { get; set; }

    // Datos de los Padres
    public Parent Madre { get; set; }
    public Parent Padre { get; set; }

    // Embarazo y Parto
    public string Gestacion { get; set; }
    public string Parto { get; set; }
    public int PesoAlNacer { get; set; }

    // Historial Médico
    public List<MedicalHistory> HistorialMedico { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
}