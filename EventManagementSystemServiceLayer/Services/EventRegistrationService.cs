using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementSystemServiceLayer.Constants;
using EventManagementSystemServiceLayer.Dtos;
using EventManagementSystemServiceLayer.Services.Administrator;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services;

public enum RegistrationServiceOutcome
{
   Confirmed,
   Waitlisted,
   DuplicateRegistration,
   DuplicateWaitlist,
   EventNotAvailable
}

public sealed class RegistrationServiceResult
{
   public RegistrationServiceOutcome Outcome { get; init; }
   public RegistrationResponseDto Response { get; init; } = new() { Outcome = string.Empty };
}

public interface IEventRegistrationService
{
   Task<RegistrationServiceResult> RegisterAsync(long attendeeUserId, RegisterEventDto dto, CancellationToken cancellationToken = default);
   Task<CancellationResponseDto> CancelRegistrationAsync(long attendeeUserId, long registrationId, CancellationToken cancellationToken = default);
}

public sealed class EventRegistrationService : IEventRegistrationService
{
private readonly IRegistrationRepository _registrationRepo;
    private readonly IPublicEventRepository _eventRepo;
    private readonly INotificationRepository _notifications;
    private readonly IAuditLoggingService _audit;
    private readonly ILogger<EventRegistrationService> _logger;

    public EventRegistrationService(
        IRegistrationRepository registrationRepo,
        IPublicEventRepository eventRepo,
        INotificationRepository notifications,
        IAuditLoggingService audit,
        ILogger<EventRegistrationService> logger)
    {
        _registrationRepo = registrationRepo;
        _eventRepo = eventRepo;
        _notifications = notifications;
        _audit = audit;
        _logger = logger;
    }

   public async Task<RegistrationServiceResult> RegisterAsync(long attendeeUserId, RegisterEventDto dto, CancellationToken cancellationToken = default)
   {
       if (dto is null) throw new ArgumentNullException(nameof(dto));
       if (dto.EventId <= 0) throw new ArgumentException("EventId is required.", nameof(dto));

       try
       {
           var result = await _registrationRepo.RegisterAttendeeWithCapacityCheckAsync(
               dto.EventId, attendeeUserId, "Self", cancellationToken);

           switch (result.Outcome)
           {
case RegistrationOutcome.Confirmed:
                    await SendNotificationAsync(attendeeUserId, "RegistrationConfirmed",
                        "Registration Confirmed",
                        $"Your registration for event #{dto.EventId} has been confirmed.",
                        dto.EventId, result.RegistrationId, cancellationToken);
                    await NotifyOrganizerOfNewRegistrationAsync(result.RegistrationId.Value, dto.EventId, cancellationToken);
                    await TryAuditAsync(attendeeUserId, AuditActionTypes.RegistrationCreated, "Registration",
                        result.RegistrationId!.Value, dto.EventId, cancellationToken);
                    return new RegistrationServiceResult
                   {
                       Outcome = RegistrationServiceOutcome.Confirmed,
                       Response = new RegistrationResponseDto
                       {
                           Outcome = nameof(RegistrationServiceOutcome.Confirmed),
                           RegistrationId = result.RegistrationId
                       }
                   };

case RegistrationOutcome.Waitlisted:
                    await SendNotificationAsync(attendeeUserId, "WaitlistAdded",
                        "Added to Waitlist",
                        $"Event #{dto.EventId} is full. You are on the waitlist at position {result.WaitlistPosition}.",
                        dto.EventId, null, cancellationToken);
                    await NotifyOrganizerOfWaitlistAsync(result.WaitlistEntryId, dto.EventId, result.WaitlistPosition, cancellationToken);
                    await TryAuditAsync(attendeeUserId, AuditActionTypes.WaitlistEntryAdded, "WaitlistEntry",
                        result.WaitlistEntryId!.Value, dto.EventId, cancellationToken);
                    return new RegistrationServiceResult
                   {
                       Outcome = RegistrationServiceOutcome.Waitlisted,
                       Response = new RegistrationResponseDto
                       {
                           Outcome = nameof(RegistrationServiceOutcome.Waitlisted),
                           WaitlistEntryId = result.WaitlistEntryId,
                           WaitlistPosition = result.WaitlistPosition
                       }
                   };

               case RegistrationOutcome.DuplicateRegistration:
                   return new RegistrationServiceResult
                   {
                       Outcome = RegistrationServiceOutcome.DuplicateRegistration,
                       Response = new RegistrationResponseDto
                       {
                           Outcome = nameof(RegistrationServiceOutcome.DuplicateRegistration),
                           Message = result.Message
                       }
                   };

               case RegistrationOutcome.DuplicateWaitlist:
                   return new RegistrationServiceResult
                   {
                       Outcome = RegistrationServiceOutcome.DuplicateWaitlist,
                       Response = new RegistrationResponseDto
                       {
                           Outcome = nameof(RegistrationServiceOutcome.DuplicateWaitlist),
                           Message = result.Message
                       }
                   };

               default:
                   return new RegistrationServiceResult
                   {
                       Outcome = RegistrationServiceOutcome.EventNotAvailable,
                       Response = new RegistrationResponseDto
                       {
                           Outcome = nameof(RegistrationServiceOutcome.EventNotAvailable),
                           Message = result.Message
                       }
                   };
           }
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "RegisterAsync failed for event {EventId} attendee {AttendeeId}.", dto.EventId, attendeeUserId);
           throw;
       }
   }

   public async Task<CancellationResponseDto> CancelRegistrationAsync(long attendeeUserId, long registrationId, CancellationToken cancellationToken = default)
   {
       try
       {
           var reg = await _registrationRepo.GetRegistrationAsync(registrationId, cancellationToken);
           if (reg is null || reg.AttendeeUserId != attendeeUserId)
           {
               return new CancellationResponseDto { Cancelled = false, Reason = "Registration not found." };
           }

           var result = await _registrationRepo.CancelRegistrationAndPromoteAsync(registrationId, attendeeUserId, cancellationToken);
           if (!result.Cancelled)
           {
               return new CancellationResponseDto { Cancelled = false, Reason = result.Reason };
           }

await SendNotificationAsync(attendeeUserId, "RegistrationCancelled",
                "Registration Cancelled",
                $"Your registration for event #{reg.EventId} has been cancelled.",
                reg.EventId, registrationId, cancellationToken);

            await TryAuditAsync(attendeeUserId, AuditActionTypes.RegistrationCancelled, "Registration",
                registrationId, reg.EventId, cancellationToken);

           if (reg.Event is not null)
           {
               await SendNotificationAsync(reg.Event.OrganizerUserId, "AttendeeCancelled",
                   "Attendee Cancelled Registration",
                   $"Attendee #{attendeeUserId} cancelled their registration for event #{reg.EventId}.",
                   reg.EventId, registrationId, cancellationToken);
           }

           if (result.PromotedAttendeeUserId.HasValue && result.PromotedRegistrationId.HasValue)
           {
               await SendNotificationAsync(result.PromotedAttendeeUserId.Value, "WaitlistPromoted",
                   "Promoted from Waitlist",
                   $"You have been promoted from the waitlist to a confirmed registration for event #{reg.EventId}.",
                   reg.EventId, result.PromotedRegistrationId, cancellationToken);
           }

           return new CancellationResponseDto
           {
               Cancelled = true,
               PromotedRegistrationId = result.PromotedRegistrationId,
               PromotedAttendeeUserId = result.PromotedAttendeeUserId
           };
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "CancelRegistrationAsync failed for registration {RegistrationId}.", registrationId);
           throw;
       }
   }

   private async Task TryAuditAsync(long actorUserId, string actionType, string targetEntity, long targetEntityId, long? eventId, CancellationToken cancellationToken)
    {
        try
        {
            await _audit.LogAuditAsync(actorUserId, actionType, targetEntity, targetEntityId, "Success", "Unknown", null, eventId);
        }
        catch { /* best-effort */ }
    }

    private async Task NotifyOrganizerOfNewRegistrationAsync(long registrationId, long eventId, CancellationToken cancellationToken)
    {
        try
        {
            var reg = await _registrationRepo.GetRegistrationAsync(registrationId, cancellationToken);
            var organizerId = reg?.Event?.OrganizerUserId;
            if (organizerId is null) return;
            var attendeeName = reg?.AttendeeUser?.DisplayName;
            await SendNotificationAsync(organizerId.Value, "NewRegistration",
                "New Registration",
                $"Attendee '{attendeeName ?? $"#{reg!.AttendeeUserId}"}' registered for event #{eventId}.",
                eventId, registrationId, cancellationToken);
        }
        catch { /* best-effort */ }
    }

    private async Task NotifyOrganizerOfWaitlistAsync(long? waitlistEntryId, long eventId, int? position, CancellationToken cancellationToken)
    {
        try
        {
            if (waitlistEntryId is null) return;
            var entry = await _registrationRepo.GetWaitlistEntryAsync(waitlistEntryId.Value, cancellationToken);
            var organizerId = entry?.Event?.OrganizerUserId;
            if (organizerId is null) return;
            var attendeeName = entry?.AttendeeUser?.DisplayName;
            await SendNotificationAsync(organizerId.Value, "NewWaitlistEntry",
                "New Waitlist Entry",
                $"Attendee '{attendeeName ?? $"#{entry!.AttendeeUserId}"}' joined the waitlist (position {position ?? 0}) for event #{eventId}.",
                eventId, null, cancellationToken);
        }
        catch { /* best-effort */ }
    }

    private Task SendNotificationAsync(long recipientUserId, string type, string title, string message,
       long? eventId, long? registrationId, CancellationToken cancellationToken)
   {
       var n = new Notification
       {
           RecipientUserId = recipientUserId,
           NotificationType = type,
           Title = title,
           Message = message,
           RelatedEventId = eventId,
           RelatedRegistrationId = registrationId,
           DeliveryStatus = "Sent",
           SentAtUtc = DateTime.UtcNow
       };
       return _notifications.CreateAsync(n, cancellationToken);
   }
}
