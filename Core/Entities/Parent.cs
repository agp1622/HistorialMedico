using System.Text.Json.Serialization;

namespace Core.Entities;

public class Parent: BaseEntity
{
    [JsonIgnore]
    public string Name {get; set;}
    public string? MidleName {get; set;}
    public string LastName {get; set;}
}