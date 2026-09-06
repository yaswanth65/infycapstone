namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class ReportFilterDto
   {
       public long? EventId { get; set; }
       public DateTime? StartDate { get; set; }
       public DateTime? EndDate { get; set; }
       public int PageNumber { get; set; } = 1;
       public int PageSize { get; set; } = 20;
   }
}
