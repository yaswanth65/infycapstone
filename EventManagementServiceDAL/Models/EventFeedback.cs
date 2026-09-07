namespace EventManagementServiceDAL.Models;

public partial class EventFeedback
{
    public long FeedbackId { get; set; }
    public long EventId { get; set; }
    public long AttendeeUserId { get; set; }
    public int Rating { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsFlagged { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual User AttendeeUser { get; set; } = null!;
}
