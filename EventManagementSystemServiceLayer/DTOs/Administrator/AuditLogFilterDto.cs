namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class AuditLogFilterDto
   {
       public long? ActorUserId { get; set; }
       public string? ActionType { get; set; }
       public DateTime? StartDate { get; set; }
       public DateTime? EndDate { get; set; }
       public int PageNumber { get; set; } = 1;
       public int PageSize { get; set; } = 10;
   }
}
