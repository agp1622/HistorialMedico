namespace HistorialMedico.Domain;

public class PaginatedList<T>
{
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; }
}