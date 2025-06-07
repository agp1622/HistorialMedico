using System.Text.Json.Serialization;

namespace Core.Entities;

public class MedicalHistory : BaseEntity
{
    [JsonIgnore]
    public string Note { get; set; }
    public DateTime LogDate { get; set; }
}