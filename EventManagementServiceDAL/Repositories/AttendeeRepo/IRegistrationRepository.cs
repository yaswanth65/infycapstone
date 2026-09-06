using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AttendeeRepo;

public enum RegistrationOutcome
{
   Confirmed,
   Waitlisted,
   DuplicateRegistration,
   DuplicateWaitlist,
   EventNotAvailable
}

public sealed class RegistrationResult
{
   public RegistrationOutcome Outcome { get; init; }
   public long? RegistrationId { get; init; }
   public long? WaitlistEntryId { get; init; }
   public int? WaitlistPosition { get; init; }
   public string? Message { get; init; }
}

public sealed class CancellationResult
{
   public bool Cancelled { get; init; }
   public string? Reason { get; init; }
   public long? PromotedRegistrationId { get; init; }
   public long? PromotedAttendeeUserId { get; init; }
   public long? PromotedFromWaitlistEntryId { get; init; }
}

public interface IRegistrationRepository
{
   Task<RegistrationResult> RegisterAttendeeWithCapacityCheckAsync(
       long eventId,
       long attendeeUserId,
       string source,
       CancellationToken cancellationToken = default);

   Task<CancellationResult> CancelRegistrationAndPromoteAsync(
       long registrationId,
       long attendeeUserId,
       CancellationToken cancellationToken = default);

   Task<Registration?> GetRegistrationAsync(long registrationId, CancellationToken cancellationToken = default);

   Task<IReadOnlyList<Registration>> GetRegistrationsForAttendeeAsync(long attendeeUserId, CancellationToken cancellationToken = default);

   Task<IReadOnlyList<WaitlistEntry>> GetActiveWaitlistForAttendeeAsync(long attendeeUserId, CancellationToken cancellationToken = default);

   Task<int> GetWaitlistPositionAsync(long waitlistEntryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Registration>> GetRegistrationsForEventAsync(long eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitlistEntry>> GetWaitlistForEventAsync(long eventId, CancellationToken cancellationToken = default);

    Task<WaitlistEntry?> GetWaitlistEntryAsync(long waitlistEntryId, CancellationToken cancellationToken = default);
}
