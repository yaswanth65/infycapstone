using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementSystemServiceLayer.Dtos;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services;

public interface IPublicEventService
{
   Task<IReadOnlyList<PublicEventDto>> GetPublicEventsAsync(PublicEventQueryDto query, CancellationToken cancellationToken = default);
   Task<PublicEventDto?> GetEventByIdAsync(long eventId, CancellationToken cancellationToken = default);
}

public sealed class PublicEventService : IPublicEventService
{
   private readonly IPublicEventRepository _repo;
   private readonly ILogger<PublicEventService> _logger;

   public PublicEventService(IPublicEventRepository repo, ILogger<PublicEventService> logger)
   {
       _repo = repo;
       _logger = logger;
   }

   public async Task<IReadOnlyList<PublicEventDto>> GetPublicEventsAsync(PublicEventQueryDto query, CancellationToken cancellationToken = default)
   {
       try
       {
           var items = await _repo.SearchPublishedEventsAsync(
               query.Keyword, query.FromUtc, query.ToUtc, query.Location, query.OnlyAvailable, cancellationToken);
           return items.Select(Map).ToList();
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "PublicEventService.GetPublicEventsAsync failed.");
           throw;
       }
   }

   public async Task<PublicEventDto?> GetEventByIdAsync(long eventId, CancellationToken cancellationToken = default)
   {
       try
       {
           var item = await _repo.GetPublishedEventByIdAsync(eventId, cancellationToken);
           return item is null ? null : Map(item);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "PublicEventService.GetEventByIdAsync failed for {EventId}.", eventId);
           throw;
       }
   }

   private static PublicEventDto Map(PublicEventListItem e) => new()
   {
       EventId = e.EventId,
       Title = e.Title,
       Description = e.Description,
       Venue = e.Venue,
       StartAtUtc = e.StartAtUtc,
       EndAtUtc = e.EndAtUtc,
       RegistrationOpenAtUtc = e.RegistrationOpenAtUtc,
       RegistrationCloseAtUtc = e.RegistrationCloseAtUtc,
       Capacity = e.Capacity,
       ConfirmedCount = e.ConfirmedCount,
       AvailableCapacity = e.AvailableCapacity,
       WaitlistCount = e.WaitlistCount,
       WaitlistAvailable = e.WaitlistAvailable,
       Status = e.Status,
       WaitlistAttendees = e.WaitlistAttendees,
       CapacityMessage = e.CapacityMessage
   };
}
