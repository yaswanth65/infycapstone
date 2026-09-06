namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class RegistrationReportItemDto
   {
       public long RegistrationId { get; set; }
       public long EventId { get; set; }
       public string EventTitle { get; set; } = null!;
       public long AttendeeUserId { get; set; }
       public string AttendeeName { get; set; } = null!;
       public string AttendeeEmail { get; set; } = null!;
       public string RegistrationStatus { get; set; } = null!;
       public string Source { get; set; } = null!;
       public DateTime RegisteredAtUtc { get; set; }
       public DateTime? CancelledAtUtc { get; set; }
       public string? CancelReason { get; set; }
   }
}
