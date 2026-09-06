namespace EventManagementServiceDAL.Models;

public partial class User
{
   public long UserId { get; set; }
   public string Email { get; set; } = null!;
   public string UserName { get; set; } = null!;
   public byte[] PasswordHash { get; set; } = null!;
   public byte[] PasswordSalt { get; set; } = null!;
   public string DisplayName { get; set; } = null!;
   public string? PhoneNumber { get; set; }
   public int RoleId { get; set; }
   public bool IsActive { get; set; }
   public DateTime? DeactivatedAtUtc { get; set; }
   public DateTime CreatedAtUtc { get; set; }
   public DateTime? UpdatedAtUtc { get; set; }
   public byte[] RowVersion { get; set; } = null!;

   public virtual ICollection<AttendanceRecord> AttendanceRecordCorrectedByUsers { get; set; } = new List<AttendanceRecord>();
   public virtual ICollection<AttendanceRecord> AttendanceRecordRecordedByUsers { get; set; } = new List<AttendanceRecord>();
   public virtual ICollection<AuditRecord> AuditRecords { get; set; } = new List<AuditRecord>();
   public virtual ICollection<EventStatusHistory> EventStatusHistories { get; set; } = new List<EventStatusHistory>();
   public virtual ICollection<Event> Events { get; set; } = new List<Event>();
   public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
   public virtual ICollection<Registration> RegistrationAttendeeUsers { get; set; } = new List<Registration>();
   public virtual ICollection<Registration> RegistrationCancelledByUsers { get; set; } = new List<Registration>();
   public virtual ICollection<RegistrationRequest> RegistrationRequestAttendeeUsers { get; set; } = new List<RegistrationRequest>();
   public virtual ICollection<RegistrationRequest> RegistrationRequestRequestedByUsers { get; set; } = new List<RegistrationRequest>();
   public virtual Role Role { get; set; } = null!;
   public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();
}
