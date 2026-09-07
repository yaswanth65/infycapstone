namespace EventManagementServiceDAL.Models;

public partial class EventCategoryMapping
{
    public long EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime AssignedAtUtc { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual EventCategory Category { get; set; } = null!;
}
