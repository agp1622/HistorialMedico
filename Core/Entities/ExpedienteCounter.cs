using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class ExpedienteCounter
{
    [Key]
    public int Year { get; set; }
    public int Counter { get; set; }
}