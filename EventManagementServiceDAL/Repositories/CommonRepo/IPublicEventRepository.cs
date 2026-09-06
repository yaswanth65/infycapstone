namespace EventManagementServiceDAL.Repositories.CommonRepo;

public interface IPublicEventRepository
{
   Task<IReadOnlyList<PublicEventListItem>> SearchPublishedEventsAsync(
       string? keyword,
       DateTime? fromUtc,
       DateTime? toUtc,
       string? location,
       bool? onlyAvailable,
       CancellationToken cancellationToken = default);

   Task<PublicEventListItem?> GetPublishedEventByIdAsync(long eventId, CancellationToken cancellationToken = default);
}

public sealed class WaitlistAttendeeItem
{
    public long AttendeeUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}

public sealed class PublicEventListItem
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
   public int ConfirmedCount { get; set; }
   public int WaitlistCount { get; set; }
   public string Status { get; set; } = null!;
   public List<WaitlistAttendeeItem> WaitlistAttendees { get; set; } = new();

   public int AvailableCapacity => Math.Max(0, Capacity - ConfirmedCount);
   public bool WaitlistAvailable => ConfirmedCount >= Capacity;
   public string CapacityMessage => ConfirmedCount >= Capacity 
       ? "Capacity is full. You will be added to the waitlist upon registration." 
       : $"Seats available ({AvailableCapacity} remaining).";
}
