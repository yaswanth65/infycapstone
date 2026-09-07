namespace EventManagementServiceDAL.Models;

public partial class EventSeries
{
    public long SeriesId { get; set; }
    public long OrganizerUserId { get; set; }
    public string RecurrencePattern { get; set; } = null!;
    public int RecurrenceInterval { get; set; } = 1;
    public int? DaysOfWeekMask { get; set; }
    public DateTime RecurrenceEndDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public virtual User OrganizerUser { get; set; } = null!;
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
