public class PaginatedList<T>
{
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; }

    public PaginatedList() 
    {
        Items = [];
        TotalRecords = 0;
        TotalPages = 1;
        CurrentPage = 1;
        PageSize = 10;
    }

    public PaginatedList(List<T> items, int totalRecords, int currentPage, int pageSize)
    {
        Items = items ?? new List<T>();
        TotalRecords = totalRecords;
        PageSize = pageSize > 0 ? pageSize : 10;
        TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
        CurrentPage = currentPage > 0 ? currentPage : 1;
    }
}