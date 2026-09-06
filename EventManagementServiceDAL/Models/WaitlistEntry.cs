namespace EventManagementServiceDAL.Models;

public partial class WaitlistEntry
{
   public long WaitlistEntryId { get; set; }
   public long EventId { get; set; }
   public long AttendeeUserId { get; set; }
   public string WaitlistStatus { get; set; } = null!;
   public DateTime QueuedAtUtc { get; set; }
   public DateTime? PromotedAtUtc { get; set; }
   public long? PromotedToRegistrationId { get; set; }
   public DateTime? RemovedAtUtc { get; set; }
   public string? RemovedReason { get; set; }

   public virtual User AttendeeUser { get; set; } = null!;
   public virtual Event Event { get; set; } = null!;
   public virtual Registration? PromotedToRegistration { get; set; }
}
