using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class MedicalHistory : BaseEntity
{
    public string Nota { get; set; }
    public DateTime Fecha { get; set; }
    
    [ForeignKey("Patient")]
    public Guid PatientId { get; set; }
    [JsonIgnore]
    public virtual Patient? Patient { get; set; }
    
}