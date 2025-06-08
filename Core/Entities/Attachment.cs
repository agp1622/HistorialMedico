using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class Attachment: BaseEntity
{
    public string Name { get; set; }
    public string Path { get; set; }
    public DateTime UploadDate { get; set; }
    public string Size { get; set; }
    [ForeignKey("Patient")]
    public Guid PatientId { get; set; }
    
    [JsonIgnore]
    public virtual Patient? Patient { get; set; }
}