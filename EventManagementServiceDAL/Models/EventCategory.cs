namespace EventManagementServiceDAL.Models;

public partial class EventCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }

    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<EventCategoryMapping> EventCategoryMappings { get; set; } = new List<EventCategoryMapping>();
    public virtual ICollection<AttendeeInterest> AttendeeInterests { get; set; } = new List<AttendeeInterest>();
}
