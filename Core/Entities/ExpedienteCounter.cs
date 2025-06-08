using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class ExpedienteCounter: BaseEntity
{
    public int Year { get; set; }
    public int Counter { get; set; }
}