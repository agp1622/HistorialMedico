using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Core.Entities;

public class Patient: BaseEntity
{
    [JsonIgnore]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Auto-generate the Id
    public Guid Id {get;set;}
    public string Name { get; set; }
    public string? MiddleName { get; set; }
    public string FirstLastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime BirthDate { get; set; }
}