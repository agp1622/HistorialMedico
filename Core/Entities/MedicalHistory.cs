using System.Text.Json.Serialization;

namespace Core.Entities;

public class MedicalHistory
{
    [JsonIgnore]
    public int Id { get; set; }
    public string? History { get; set; }
    public string currentAge { get; set; }
    public bool IsPremature { get; set; }
    public List<string> MedicalConditions { get; set; }
}