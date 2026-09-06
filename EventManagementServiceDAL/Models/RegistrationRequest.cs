namespace EventManagementServiceDAL.Models;

public partial class RegistrationRequest
{
   public long RegistrationRequestId { get; set; }
   public long EventId { get; set; }
   public long AttendeeUserId { get; set; }
   public long RequestedByUserId { get; set; }
   public string RequestType { get; set; } = null!;
   public string RequestStatus { get; set; } = null!;
   public DateTime RequestedAtUtc { get; set; }
   public DateTime? RespondedAtUtc { get; set; }
   public string? ResponseComment { get; set; }
   public long? LinkedRegistrationId { get; set; }

   public virtual User AttendeeUser { get; set; } = null!;
   public virtual Event Event { get; set; } = null!;
   public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
   public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
   public virtual User RequestedByUser { get; set; } = null!;
}
