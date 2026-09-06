using EventManagementServiceDAL.Repositories.CommonRepo;

namespace EventManagementSystemServiceLayer.Dtos;

public sealed class PublicEventQueryDto
{
   public string? Keyword { get; set; }
   public DateTime? FromUtc { get; set; }
   public DateTime? ToUtc { get; set; }
   public string? Location { get; set; }
   public bool? OnlyAvailable { get; set; }
}

public sealed class PublicEventDto
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
   public int AvailableCapacity { get; set; }
   public int WaitlistCount { get; set; }
   public bool WaitlistAvailable { get; set; }
   public string Status { get; set; } = null!;
   public List<WaitlistAttendeeItem> WaitlistAttendees { get; set; } = new();
   public string CapacityMessage { get; set; } = string.Empty;
}

public sealed class RegisterEventDto
{
   public long EventId { get; set; }
}

public sealed class RegistrationResponseDto
{
   public string Outcome { get; set; } = null!;
   public long? RegistrationId { get; set; }
   public long? WaitlistEntryId { get; set; }
   public int? WaitlistPosition { get; set; }
   public string? Message { get; set; }
}

public sealed class CancellationResponseDto
{
   public bool Cancelled { get; set; }
   public string? Reason { get; set; }
   public long? PromotedRegistrationId { get; set; }
   public long? PromotedAttendeeUserId { get; set; }
}

public sealed class MyEventItemDto
{
   public long EventId { get; set; }
   public string Title { get; set; } = null!;
   public string Venue { get; set; } = null!;
   public DateTime StartAtUtc { get; set; }
   public DateTime EndAtUtc { get; set; }
   public long? RegistrationId { get; set; }
   public long? WaitlistEntryId { get; set; }
   public string ItemType { get; set; } = null!;
   public string Status { get; set; } = null!;
   public int? WaitlistPosition { get; set; }
   public string? AttendanceStatus { get; set; }
   public bool IsPast { get; set; }
}

public sealed class MyEventsDto
{
   public List<MyEventItemDto> Active { get; set; } = new();
   public List<MyEventItemDto> Past { get; set; } = new();
}
