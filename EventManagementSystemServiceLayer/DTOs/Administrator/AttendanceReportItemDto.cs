namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class AttendanceReportItemDto
   {
       public long AttendanceRecordId { get; set; }
       public long RegistrationId { get; set; }
       public long EventId { get; set; }
       public string EventTitle { get; set; } = null!;
       public long AttendeeUserId { get; set; }
       public string AttendeeName { get; set; } = null!;
       public string AttendeeEmail { get; set; } = null!;
       public string AttendanceStatus { get; set; } = null!;
       public DateTime RecordedAtUtc { get; set; }
       public bool IsFinalized { get; set; }
       public DateTime? FinalizedAtUtc { get; set; }
       public string? CorrectionReason { get; set; }
   }
}
