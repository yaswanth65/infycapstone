namespace EventManagementServiceDAL.Models;

public partial class Notification
{
   public long NotificationId { get; set; }
   public long RecipientUserId { get; set; }
   public string NotificationType { get; set; } = null!;
   public string Title { get; set; } = null!;
   public string Message { get; set; } = null!;
   public long? RelatedEventId { get; set; }
   public long? RelatedRegistrationId { get; set; }
   public long? RelatedRequestId { get; set; }
   public string DeliveryStatus { get; set; } = null!;
   public DateTime? ScheduledAtUtc { get; set; }
   public DateTime? SentAtUtc { get; set; }
   public DateTime? ReadAtUtc { get; set; }
   public DateTime CreatedAtUtc { get; set; }

   public virtual User RecipientUser { get; set; } = null!;
   public virtual Event? RelatedEvent { get; set; }
   public virtual Registration? RelatedRegistration { get; set; }
   public virtual RegistrationRequest? RelatedRequest { get; set; }
}
