using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.Constants;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.EventManager;
using EventManagementSystemServiceLayer.Services.Administrator;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.EventManager
{
   public interface IEventManagementService
   {
       Task<EventResponseDto?> CreateAsync(long organizerUserId, EventCreateDto dto, string ipAddress, CancellationToken ct = default);
       Task<EventResponseDto?> UpdateAsync(long callerUserId, EventUpdateDto dto, string ipAddress, CancellationToken ct = default);
       Task<EventResponseDto?> PublishAsync(long callerUserId, long eventId, string? remarks, string ipAddress, CancellationToken ct = default);
       Task<EventResponseDto?> CloseAsync(long callerUserId, long eventId, string? remarks, string ipAddress, CancellationToken ct = default);
       Task<EventResponseDto?> CancelAsync(long callerUserId, long eventId, string? remarks, string ipAddress, CancellationToken ct = default);
       Task<EventResponseDto?> GetAsync(long eventId, CancellationToken ct = default);
Task<PaginatedResponse<EventResponseDto>> ListForOrganizerAsync(long organizerUserId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<EventRosterResponseDto?> GetEventRosterAsync(long callerUserId, long eventId, CancellationToken ct = default);
    }

   public sealed class EventManagementService : IEventManagementService
   {
       private const string DraftStatus = "Draft";
       private const string PublishedStatus = "Published";
       private const string ClosedStatus = "Closed";
       private const string CancelledStatus = "Cancelled";

       private readonly IEventRepository _events;
       private readonly IRegistrationRepository _registrations;
       private readonly INotificationRepository _notifications;
       private readonly IVenueRepository _venues;
       private readonly ICategoryRepository _categories;
       private readonly IAuditLoggingService _audit;
       private readonly ILogger<EventManagementService> _logger;

       public EventManagementService(
           IEventRepository events,
           IRegistrationRepository registrations,
           INotificationRepository notifications,
           IVenueRepository venues,
           ICategoryRepository categories,
           IAuditLoggingService audit,
           ILogger<EventManagementService> logger)
       {
           _events = events;
           _registrations = registrations;
           _notifications = notifications;
           _venues = venues;
           _categories = categories;
           _audit = audit;
           _logger = logger;
       }

       public async Task<EventResponseDto?> CreateAsync(long organizerUserId, EventCreateDto dto, string ipAddress, CancellationToken ct = default)
       {
           try
           {
               if (dto.VenueId.HasValue && dto.VenueId.Value > 0)
               {
                   var hasConflict = await _venues.HasScheduleOverlapAsync(dto.VenueId.Value, dto.StartAtUtc, dto.EndAtUtc, null, ct);
                   if (hasConflict)
                   {
                       throw new InvalidOperationException("Venue conflict: The selected venue is already booked for an overlapping time window.");
                   }
               }

               var now = DateTime.UtcNow;
               var entity = new Event
               {
                   Title = dto.Title,
                   Description = dto.Description,
                   Venue = dto.Venue,
                   VenueId = dto.VenueId > 0 ? dto.VenueId : null,
                   StartAtUtc = dto.StartAtUtc,
                   EndAtUtc = dto.EndAtUtc,
                   RegistrationOpenAtUtc = dto.RegistrationOpenAtUtc,
                   RegistrationCloseAtUtc = dto.RegistrationCloseAtUtc,
                   Capacity = dto.Capacity,
                   Status = DraftStatus,
                   ApprovalStatus = "Draft",
                   IsVirtual = dto.IsVirtual,
                   VirtualMeetingUrl = dto.VirtualMeetingUrl,
                   OrganizerUserId = organizerUserId,
                   CreatedAtUtc = now
               };
               var created = await _events.AddAsync(entity, ct);

               if (dto.CategoryIds != null && dto.CategoryIds.Any())
               {
                   await _categories.AssignCategoriesToEventAsync(created.EventId, dto.CategoryIds, ct);
               }

               await _events.AddStatusHistoryAsync(new EventStatusHistory
               {
                   EventId = created.EventId,
                   FromStatus = null,
                   ToStatus = DraftStatus,
                   ChangedByUserId = organizerUserId,
                   ChangedAtUtc = now,
                   Remarks = "Event created."
               }, ct);
               await _audit.LogAuditAsync(organizerUserId, AuditActionTypes.EventCreated, "Event", created.EventId, "Success", ipAddress, null, created.EventId);
               return await MapAsync(created, ct);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "CreateAsync failed for organizer {OrganizerId}.", organizerUserId);
               return null;
           }
       }

       public async Task<EventResponseDto?> UpdateAsync(long callerUserId, EventUpdateDto dto, string ipAddress, CancellationToken ct = default)
       {
           try
           {
               var existing = await _events.GetByIdAsync(dto.EventId, ct: ct);
               if (existing is null) return null;
               if (existing.Status is ClosedStatus or CancelledStatus)
                   throw new InvalidOperationException("Cannot update an event that is Closed or Cancelled.");

               if (dto.VenueId.HasValue && dto.VenueId.Value > 0)
               {
                   var hasConflict = await _venues.HasScheduleOverlapAsync(dto.VenueId.Value, dto.StartAtUtc, dto.EndAtUtc, existing.EventId, ct);
                   if (hasConflict)
                   {
                       throw new InvalidOperationException("Venue conflict: The selected venue is already booked for an overlapping time window.");
                   }
               }

               existing.Title = dto.Title;
               existing.Description = dto.Description;
               existing.Venue = dto.Venue;
               existing.VenueId = dto.VenueId > 0 ? dto.VenueId : null;
               existing.StartAtUtc = dto.StartAtUtc;
               existing.EndAtUtc = dto.EndAtUtc;
               existing.RegistrationOpenAtUtc = dto.RegistrationOpenAtUtc;
               existing.RegistrationCloseAtUtc = dto.RegistrationCloseAtUtc;
               existing.Capacity = dto.Capacity;
               existing.IsVirtual = dto.IsVirtual;
               existing.VirtualMeetingUrl = dto.VirtualMeetingUrl;

               await _events.UpdateAsync(existing, ct);

               if (dto.CategoryIds != null)
               {
                   await _categories.AssignCategoriesToEventAsync(existing.EventId, dto.CategoryIds, ct);
               }

               await _audit.LogAuditAsync(callerUserId, AuditActionTypes.EventCreated, "Event", existing.EventId, "Success", ipAddress, "{\"op\":\"update\"}", existing.EventId);
               return await MapAsync(existing, ct);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "UpdateAsync failed for event {EventId}.", dto.EventId);
               return null;
           }
       }

       public Task<EventResponseDto?> PublishAsync(long callerUserId, long eventId, string? remarks, string ipAddress, CancellationToken ct = default)
           => TransitionAsync(callerUserId, eventId, PublishedStatus, AuditActionTypes.EventPublished, remarks, ipAddress, ct);

       public Task<EventResponseDto?> CloseAsync(long callerUserId, long eventId, string? remarks, string ipAddress, CancellationToken ct = default)
           => TransitionAsync(callerUserId, eventId, ClosedStatus, AuditActionTypes.EventClosed, remarks, ipAddress, ct);

       public Task<EventResponseDto?> CancelAsync(long callerUserId, long eventId, string? remarks, string ipAddress, CancellationToken ct = default)
           => TransitionAsync(callerUserId, eventId, CancelledStatus, AuditActionTypes.EventCancelled, remarks, ipAddress, ct);

       public async Task<EventResponseDto?> GetAsync(long eventId, CancellationToken ct = default)
       {
           var e = await _events.GetByIdAsync(eventId, ct: ct);
           return e is null ? null : await MapAsync(e, ct);
       }

       public async Task<PaginatedResponse<EventResponseDto>> ListForOrganizerAsync(long organizerUserId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default)
       {
           if (pageNumber < 1) pageNumber = 1;
           if (pageSize < 1) pageSize = 20;
           if (pageSize > 100) pageSize = 100;

           var list = await _events.GetByOrganizerAsync(organizerUserId, statusFilter, pageNumber, pageSize, ct);
           var total = await _events.GetByOrganizerCountAsync(organizerUserId, statusFilter, ct);
           var data = new List<EventResponseDto>(list.Count);
           foreach (var e in list) data.Add(await MapAsync(e, ct));
           return new PaginatedResponse<EventResponseDto>
           {
               Data = data,
               PageNumber = pageNumber,
               PageSize = pageSize,
               TotalRecords = total,
               TotalPages = (int)Math.Ceiling(total / (double)pageSize)
           };
       }

public async Task<EventRosterResponseDto?> GetEventRosterAsync(long callerUserId, long eventId, CancellationToken ct = default)
        {
            try
            {
                var e = await _events.GetByIdAsync(eventId, ct: ct);
                if (e is null) return null;
                if (e.OrganizerUserId != callerUserId)
                    throw new InvalidOperationException("You are not the organizer of this event.");

                var registrations = await _registrations.GetRegistrationsForEventAsync(eventId, ct);
                var waitlist = await _registrations.GetWaitlistForEventAsync(eventId, ct);

                return new EventRosterResponseDto
                {
                    EventId = e.EventId,
                    EventTitle = e.Title,
                    Capacity = e.Capacity,
                    ConfirmedCount = registrations.Count,
                    Registrations = registrations.Select(r => new EventRosterItemDto
                    {
                        RegistrationId = r.RegistrationId,
                        EventId = r.EventId,
                        AttendeeUserId = r.AttendeeUserId,
                        AttendeeDisplayName = r.AttendeeUser?.DisplayName ?? string.Empty,
                        AttendeeEmail = r.AttendeeUser?.Email,
                        RegistrationStatus = r.RegistrationStatus,
                        Source = r.Source,
                        RegisteredAtUtc = r.RegisteredAtUtc,
                        AttendanceStatus = r.AttendanceRecord?.AttendanceStatus,
                        AttendanceFinalized = r.AttendanceRecord?.IsFinalized ?? false
                    }).ToList(),
                    Waitlist = waitlist.Select((w, i) => new EventWaitlistItemDto
                    {
                        WaitlistEntryId = w.WaitlistEntryId,
                        EventId = w.EventId,
                        AttendeeUserId = w.AttendeeUserId,
                        AttendeeDisplayName = w.AttendeeUser?.DisplayName ?? string.Empty,
                        AttendeeEmail = w.AttendeeUser?.Email,
                        Position = i + 1,
                        QueuedAtUtc = w.QueuedAtUtc
                    }).ToList()
                };
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventRosterAsync failed for event {EventId}.", eventId);
                return null;
            }
        }

        private async Task<EventResponseDto?> TransitionAsync(long callerUserId, long eventId, string targetStatus, string auditAction, string? remarks, string ipAddress, CancellationToken ct)
       {
           try
           {
               var existing = await _events.GetByIdAsync(eventId, ct: ct);
               if (existing is null) return null;

               if (!IsValidTransition(existing.Status, targetStatus))
                   throw new InvalidOperationException($"Illegal status transition from '{existing.Status}' to '{targetStatus}'.");

               if (targetStatus == PublishedStatus && existing.ApprovalStatus != "Approved")
               {
                   throw new InvalidOperationException("Event cannot be published until it has been approved by an administrator.");
               }

               var now = DateTime.UtcNow;
               var previous = existing.Status;
               existing.Status = targetStatus;

               if (targetStatus == PublishedStatus) existing.PublishedAtUtc = now;
               if (targetStatus == ClosedStatus) existing.ClosedAtUtc = now;
               if (targetStatus == CancelledStatus) existing.CancelledAtUtc = now;

               await _events.UpdateAsync(existing, ct);
               await _events.AddStatusHistoryAsync(new EventStatusHistory
               {
                   EventId = existing.EventId,
                   FromStatus = previous,
                   ToStatus = targetStatus,
                   ChangedByUserId = callerUserId,
                   ChangedAtUtc = now,
                   Remarks = remarks
               }, ct);

               await _audit.LogAuditAsync(callerUserId, auditAction, "Event", existing.EventId, "Success", ipAddress, remarks is null ? null : $"{{\"remarks\":\"{Escape(remarks)}\"}}", existing.EventId);

               await SafeNotify(existing.OrganizerUserId, targetStatus switch
               {
                   PublishedStatus => "EventPublished",
                   ClosedStatus => "EventClosed",
                   CancelledStatus => "EventCancelled",
                   _ => "EventUpdated"
               }, $"Event {targetStatus}", $"Event #{existing.EventId} moved to {targetStatus}.", existing.EventId, ct);

               if (targetStatus == CancelledStatus)
               {
                   var registrations = await _registrations.GetRegistrationsForEventAsync(existing.EventId, ct);
                   foreach (var reg in registrations)
                   {
                       await SafeNotify(reg.AttendeeUserId, "EventCancelled", "Event Cancelled", $"The event '{existing.Title}' has been cancelled by the organizer.", existing.EventId, ct);
                   }
                   var waitlist = await _registrations.GetWaitlistForEventAsync(existing.EventId, ct);
                   foreach (var w in waitlist)
                   {
                       await SafeNotify(w.AttendeeUserId, "EventCancelled", "Event Cancelled", $"The waitlisted event '{existing.Title}' has been cancelled by the organizer.", existing.EventId, ct);
                   }
               }

               return await MapAsync(existing, ct);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "TransitionAsync failed for event {EventId} -> {Target}.", eventId, targetStatus);
               return null;
           }
       }

       private static bool IsValidTransition(string current, string target) => (current, target) switch
       {
           (DraftStatus, PublishedStatus) => true,
           (DraftStatus, CancelledStatus) => true,
           (PublishedStatus, ClosedStatus) => true,
           (PublishedStatus, CancelledStatus) => true,
           _ => false
       };

       private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

       private async Task<EventResponseDto> MapAsync(Event e, CancellationToken ct)
       {
           var confirmed = await _events.GetConfirmedRegistrationCountAsync(e.EventId, ct);
           var waitlist = await _events.GetWaitlistCountAsync(e.EventId, ct);
           var catIds = (await _categories.GetCategoryIdsForEventAsync(e.EventId, ct)).ToList();
           var allCats = await _categories.GetAllAsync(false, ct);
           var catDict = allCats.ToDictionary(c => c.CategoryId, c => c.CategoryName);
           var catNames = catIds.Where(id => catDict.ContainsKey(id)).Select(id => catDict[id]).ToList();

            return new EventResponseDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                Venue = e.Venue,
                VenueId = e.VenueId,
                StartAtUtc = e.StartAtUtc,
                EndAtUtc = e.EndAtUtc,
                RegistrationOpenAtUtc = e.RegistrationOpenAtUtc,
                RegistrationCloseAtUtc = e.RegistrationCloseAtUtc,
                Capacity = e.Capacity,
                Status = e.Status,
                ApprovalStatus = e.ApprovalStatus,
                RejectionReason = e.RejectionReason,
                OrganizerUserId = e.OrganizerUserId,
                OrganizerDisplayName = e.OrganizerUser?.DisplayName,
                PublishedAtUtc = e.PublishedAtUtc,
                ClosedAtUtc = e.ClosedAtUtc,
                CancelledAtUtc = e.CancelledAtUtc,
                CreatedAtUtc = e.CreatedAtUtc,
                UpdatedAtUtc = e.UpdatedAtUtc,
                ConfirmedRegistrations = confirmed,
                WaitlistCount = waitlist,
                CategoryIds = catIds,
                Categories = catNames,
                IsVirtual = e.IsVirtual,
                VirtualMeetingUrl = e.VirtualMeetingUrl
            };
       }

       private async Task SafeNotify(long recipient, string type, string title, string message, long eventId, CancellationToken ct)
       {
           try
           {
               await _notifications.CreateAsync(new Notification
               {
                   RecipientUserId = recipient,
                   NotificationType = type,
                   Title = title,
                   Message = message,
                   RelatedEventId = eventId,
                   DeliveryStatus = "Sent",
                   SentAtUtc = DateTime.UtcNow
               }, ct);
           }
           catch { /* best-effort */ }
       }
   }
}
