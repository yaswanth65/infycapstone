namespace EventManagementServiceDAL.Models;

public partial class Registration
{
   public long RegistrationId { get; set; }
   public long EventId { get; set; }
   public long AttendeeUserId { get; set; }
   public string RegistrationStatus { get; set; } = null!;
   public string Source { get; set; } = null!;
   public long? RegistrationRequestId { get; set; }
   public DateTime RegisteredAtUtc { get; set; }
   public DateTime? CancelledAtUtc { get; set; }
   public long? CancelledByUserId { get; set; }
   public string? CancelReason { get; set; }
   public byte[] RowVersion { get; set; } = null!;

   public virtual AttendanceRecord? AttendanceRecord { get; set; }
   public virtual User AttendeeUser { get; set; } = null!;
   public virtual User? CancelledByUser { get; set; }
   public virtual Event Event { get; set; } = null!;
   public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
   public virtual RegistrationRequest? RegistrationRequest { get; set; }
   public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();
}
