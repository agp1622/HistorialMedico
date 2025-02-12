namespace HistorialMedico.Domain;

public class Patient
{
    public string Id {get;set;}
    public string Name { get; set; }
    public string? MiddleName { get; set; }
    public string FirstLastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime BirthDate { get; set; }
}