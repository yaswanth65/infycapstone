namespace EventManagementSystemServiceLayer.DTOs
{
   public class PaginatedResponse<T>
   {
       public List<T> Data { get; set; } = new();
       public int PageNumber { get; set; }
       public int PageSize { get; set; }
       public int TotalRecords { get; set; }
       public int TotalPages { get; set; }
       public bool HasNextPage => PageNumber < TotalPages;
       public bool HasPreviousPage => PageNumber > 1;
   }
}
