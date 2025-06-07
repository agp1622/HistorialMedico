using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

public class Attachment: BaseEntity
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public DateTime UploadDate { get; set; }
    
    public int PatientId { get; set; }
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; }
}