using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class AdditionalPhone: BaseEntity
{
    [Phone]
    public string Number { get; set; }
    public Guid PatientId { get; set; }
    [JsonIgnore]
    public virtual Patient? Patient { get; set; }}