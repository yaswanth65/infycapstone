namespace EventManagementServiceDAL.Models;

public partial class Venue
{
    public long VenueId { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public int Capacity { get; set; }
    public string? ContactDetails { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
