using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Entities;

public class Patient : BaseEntity
{
    public string NumeroExpediente { get; set; } = string.Empty;
    
    [Required]
    public string Nombre { get; set; } = string.Empty;
    
    public string Sexo { get; set; } = string.Empty;
    
    public string Edad { get; set; } = string.Empty;
    
    public string Diagnostico { get; set; } = string.Empty;
    
    public string ReferidoPor { get; set; } = string.Empty;
    
    [DataType(DataType.Date)]
    public string FechaNacimiento { get; set; } = string.Empty; // ISO date string
    
    [DataType(DataType.Date)]
    public string FechaConsulta { get; set; } = string.Empty;   // ISO date string
    
    public string SeguroMedico { get; set; } = string.Empty;
    
    public string Alergias { get; set; } = string.Empty;
    
    public string Madre { get; set; } = string.Empty;
    
    [Phone]
    public string MadreTelefono { get; set; } = string.Empty;
    
    [EmailAddress]
    public string MadreCorreo { get; set; } = string.Empty;
    
    public string Padre { get; set; } = string.Empty;
    
    [Phone]
    public string PadreTelefono { get; set; } = string.Empty;
    
    [EmailAddress]
    public string PadreCorreo { get; set; } = string.Empty;
    
    public string Gestacion { get; set; } = string.Empty;
    
    public string Parto { get; set; } = string.Empty;
    
    public string PesoAlNacer { get; set; } = string.Empty;
    
    public string PesoUnidad { get; set; } = string.Empty;
    
    public List<MedicalHistory> Historial { get; set; } = new();
    
    public List<Attachment> Attachments { get; set; } = new();
}