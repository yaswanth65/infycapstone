using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementSystemServiceLayer.Dtos;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services;

public interface IAttendeeHistoryService
{
   Task<MyEventsDto> GetMyEventsAsync(long attendeeUserId, CancellationToken cancellationToken = default);
}

public sealed class AttendeeHistoryService : IAttendeeHistoryService
{
   private readonly IRegistrationRepository _repo;
   private readonly ILogger<AttendeeHistoryService> _logger;

   public AttendeeHistoryService(IRegistrationRepository repo, ILogger<AttendeeHistoryService> logger)
   {
       _repo = repo;
       _logger = logger;
   }

   public async Task<MyEventsDto> GetMyEventsAsync(long attendeeUserId, CancellationToken cancellationToken = default)
   {
       try
       {
           var registrations = await _repo.GetRegistrationsForAttendeeAsync(attendeeUserId, cancellationToken);
           var waitlists = await _repo.GetActiveWaitlistForAttendeeAsync(attendeeUserId, cancellationToken);
           var now = DateTime.UtcNow;

           var result = new MyEventsDto();

           foreach (var r in registrations)
           {
               var ev = r.Event;
               var isPast = ev.EndAtUtc <= now;
               var item = new MyEventItemDto
               {
                   EventId = ev.EventId,
                   Title = ev.Title,
                   Venue = ev.Venue,
                   StartAtUtc = ev.StartAtUtc,
                   EndAtUtc = ev.EndAtUtc,
                   RegistrationId = r.RegistrationId,
                   ItemType = "Registration",
                   Status = r.RegistrationStatus,
                   AttendanceStatus = r.AttendanceRecord?.AttendanceStatus,
                   IsPast = isPast
               };
               if (isPast) result.Past.Add(item); else result.Active.Add(item);
           }

           foreach (var w in waitlists)
           {
               var position = await _repo.GetWaitlistPositionAsync(w.WaitlistEntryId, cancellationToken);
               result.Active.Add(new MyEventItemDto
               {
                   EventId = w.EventId,
                   Title = w.Event.Title,
                   Venue = w.Event.Venue,
                   StartAtUtc = w.Event.StartAtUtc,
                   EndAtUtc = w.Event.EndAtUtc,
                   WaitlistEntryId = w.WaitlistEntryId,
                   ItemType = "Waitlist",
                   Status = w.WaitlistStatus,
                   WaitlistPosition = position,
                   IsPast = false
               });
           }

           return result;
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "GetMyEventsAsync failed for attendee {AttendeeId}.", attendeeUserId);
           throw;
       }
   }
}
