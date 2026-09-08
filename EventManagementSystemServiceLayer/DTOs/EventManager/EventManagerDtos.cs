namespace EventManagementSystemServiceLayer.DTOs.EventManager
{
   public class EventCreateDto
   {
       public string Title { get; set; } = null!;
       public string? Description { get; set; }
       public string Venue { get; set; } = null!;
       public DateTime StartAtUtc { get; set; }
       public DateTime EndAtUtc { get; set; }
       public DateTime? RegistrationOpenAtUtc { get; set; }
       public DateTime? RegistrationCloseAtUtc { get; set; }
       public int Capacity { get; set; }
   }

   public class EventUpdateDto
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
   }

   public class EventStatusTransitionDto
   {
       public long EventId { get; set; }
       public string? Remarks { get; set; }
   }

   public class EventResponseDto
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
        public string? ApprovalStatus { get; set; }
        public string? RejectionReason { get; set; }
        public long OrganizerUserId { get; set; }
       public string? OrganizerDisplayName { get; set; }
       public DateTime? PublishedAtUtc { get; set; }
       public DateTime? ClosedAtUtc { get; set; }
       public DateTime? CancelledAtUtc { get; set; }
       public DateTime CreatedAtUtc { get; set; }
       public DateTime? UpdatedAtUtc { get; set; }
       public int ConfirmedRegistrations { get; set; }
       public int WaitlistCount { get; set; }
   }

   public class AttendanceRecordDto
   {
       public long RegistrationId { get; set; }
       public string AttendanceStatus { get; set; } = null!;
       public bool Finalize { get; set; }
   }

   public class AttendanceCorrectionDto
   {
       public string AttendanceStatus { get; set; } = null!;
       public string? CorrectionReason { get; set; }
   }

   public class AttendanceResponseDto
   {
       public long AttendanceRecordId { get; set; }
       public long RegistrationId { get; set; }
       public long EventId { get; set; }
       public string AttendanceStatus { get; set; } = null!;
       public long RecordedByUserId { get; set; }
       public DateTime RecordedAtUtc { get; set; }
       public bool IsFinalized { get; set; }
       public DateTime? FinalizedAtUtc { get; set; }
       public long? CorrectedByUserId { get; set; }
       public DateTime? CorrectedAtUtc { get; set; }
       public string? CorrectionReason { get; set; }
       public int RevisionNo { get; set; }
   }

   public class RegistrationRequestCreateDto
   {
       public long EventId { get; set; }
       public long AttendeeUserId { get; set; }
   }

   public class RegistrationRequestDecisionDto
   {
       public string Decision { get; set; } = null!;
       public string? ResponseComment { get; set; }
   }

   public class RegistrationRequestResponseDto
   {
       public long RegistrationRequestId { get; set; }
       public long EventId { get; set; }
       public long AttendeeUserId { get; set; }
       public string AttendeeDisplayName { get; set; } = string.Empty;
       public long RequestedByUserId { get; set; }
       public string RequestType { get; set; } = null!;
       public string RequestStatus { get; set; } = null!;
       public DateTime RequestedAtUtc { get; set; }
       public DateTime? RespondedAtUtc { get; set; }
       public string? ResponseComment { get; set; }
       public long? LinkedRegistrationId { get; set; }
   }

   public class EventRosterItemDto
    {
        public long RegistrationId { get; set; }
        public long EventId { get; set; }
        public long AttendeeUserId { get; set; }
        public string AttendeeDisplayName { get; set; } = string.Empty;
        public string? AttendeeEmail { get; set; }
        public string RegistrationStatus { get; set; } = null!;
        public string? Source { get; set; }
        public DateTime RegisteredAtUtc { get; set; }
        public string? AttendanceStatus { get; set; }
        public bool AttendanceFinalized { get; set; }
    }

    public class EventWaitlistItemDto
    {
        public long WaitlistEntryId { get; set; }
        public long EventId { get; set; }
        public long AttendeeUserId { get; set; }
        public string AttendeeDisplayName { get; set; } = string.Empty;
        public string? AttendeeEmail { get; set; }
        public int Position { get; set; }
        public DateTime QueuedAtUtc { get; set; }
    }

    public class EventRosterResponseDto
    {
        public long EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int ConfirmedCount { get; set; }
        public List<EventRosterItemDto> Registrations { get; set; } = new();
        public List<EventWaitlistItemDto> Waitlist { get; set; } = new();
    }

    public class RoleResponseDto
   {
       public int RoleId { get; set; }
       public string RoleName { get; set; } = null!;
       public bool IsActive { get; set; }
       public DateTime CreatedAtUtc { get; set; }
   }
}
