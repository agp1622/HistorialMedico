namespace Presentation.Domain;

public class PaginationQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}