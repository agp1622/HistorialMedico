using System.Text.Json.Serialization;

namespace Core.Entities;

public class Parent: BaseEntity
{
    public string Nombre {get; set;}
    public string Telefono { get; set; }
    public string Email { get; set; }
}