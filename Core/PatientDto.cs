using System.ComponentModel.DataAnnotations;
using Core.Entities;

namespace Core;

public class PatientDto {
    public Guid Id { get; set; } = Guid.NewGuid();
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
    
    public List<AdditionalPhone> AdditionalPhones { get; set; } = new();
}
