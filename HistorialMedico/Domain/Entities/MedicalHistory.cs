namespace HistorialMedico.Domain;

public class MedicalHistory
{
    public int Id { get; set; }
    public string? History { get; set; }
    public string currentAge { get; set; }
    public bool IsPremature { get; set; }
    public List<string> MedicalConditions { get; set; }
}