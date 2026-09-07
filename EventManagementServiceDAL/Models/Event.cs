namespace EventManagementServiceDAL.Models;

public partial class Event
{
    public long EventId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Venue { get; set; } = null!;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public DateTime? RegistrationOpenAtUtc { get; set; }
    public DateTime? RegistrationCloseAtUtc { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = null!;
    public long OrganizerUserId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    // Brownfield Extensions
    public long? VenueId { get; set; }
    public long? SeriesId { get; set; }
    public bool IsVirtual { get; set; }
    public string? VirtualMeetingUrl { get; set; }
    public string ApprovalStatus { get; set; } = "Approved"; // Draft, Submitted, Approved, Rejected
    public string? RejectionReason { get; set; }

    public virtual Venue? VenueNavigation { get; set; }
    public virtual EventSeries? SeriesNavigation { get; set; }
    public virtual ICollection<EventCategoryMapping> EventCategoryMappings { get; set; } = new List<EventCategoryMapping>();
    public virtual ICollection<EventApprovalRequest> EventApprovalRequests { get; set; } = new List<EventApprovalRequest>();
    public virtual ICollection<EventCapacityAlertConfig> EventCapacityAlertConfigs { get; set; } = new List<EventCapacityAlertConfig>();
    public virtual ICollection<EventFeedback> EventFeedbacks { get; set; } = new List<EventFeedback>();

    public virtual ICollection<AuditRecord> AuditRecords { get; set; } = new List<AuditRecord>();
    public virtual ICollection<EventStatusHistory> EventStatusHistories { get; set; } = new List<EventStatusHistory>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual User OrganizerUser { get; set; } = null!;
    public virtual ICollection<RegistrationRequest> RegistrationRequests { get; set; } = new List<RegistrationRequest>();
    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();
}
