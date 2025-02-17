using System.Text.Json.Serialization;

namespace Core.Entities;

public class Parent
{
    [JsonIgnore]
    public int Id {get; set;}
    public string Name {get; set;}
    public string? MidleName {get; set;}
    public string LastName {get; set;}
}