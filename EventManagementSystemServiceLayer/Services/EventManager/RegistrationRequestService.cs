using EventManagementServiceDAL.Models;
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
   public interface IRegistrationRequestService
   {
       Task<RegistrationRequestResponseDto?> CreateAsync(long requestedByUserId, RegistrationRequestCreateDto dto, string ipAddress, CancellationToken ct = default);
       Task<RegistrationRequestResponseDto?> DecideAsync(long callerUserId, long requestId, RegistrationRequestDecisionDto dto, string ipAddress, CancellationToken ct = default);
       Task<PaginatedResponse<RegistrationRequestResponseDto>> ListByEventAsync(long eventId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default);
   }

   public sealed class RegistrationRequestService : IRegistrationRequestService
   {
       private const string OnBehalf = "OnBehalf";
       private const string PendingStatus = "Pending";
       private const string AcceptedStatus = "Accepted";
       private const string RejectedStatus = "Rejected";

       private readonly IRegistrationRequestRepository _requests;
       private readonly IRegistrationRepository _registrationRepo;
       private readonly INotificationRepository _notifications;
       private readonly IAuditLoggingService _audit;
       private readonly ILogger<RegistrationRequestService> _logger;

       public RegistrationRequestService(
           IRegistrationRequestRepository requests,
           IRegistrationRepository registrationRepo,
           INotificationRepository notifications,
           IAuditLoggingService audit,
           ILogger<RegistrationRequestService> logger)
       {
           _requests = requests;
           _registrationRepo = registrationRepo;
           _notifications = notifications;
           _audit = audit;
           _logger = logger;
       }

       public async Task<RegistrationRequestResponseDto?> CreateAsync(long requestedByUserId, RegistrationRequestCreateDto dto, string ipAddress, CancellationToken ct = default)
       {
           try
           {
               if (await _requests.HasPendingAsync(dto.EventId, dto.AttendeeUserId, ct))
                   throw new InvalidOperationException("A pending request already exists for this attendee on this event.");

               var req = new RegistrationRequest
               {
                   EventId = dto.EventId,
                   AttendeeUserId = dto.AttendeeUserId,
                   RequestedByUserId = requestedByUserId,
                   RequestType = OnBehalf,
                   RequestStatus = PendingStatus,
                   RequestedAtUtc = DateTime.UtcNow
               };
               var created = await _requests.AddAsync(req, ct);
               await _audit.LogAuditAsync(requestedByUserId, AuditActionTypes.RegistrationRequestCreated, "RegistrationRequest", created.RegistrationRequestId, "Success", ipAddress, null, dto.EventId);
               await SafeNotify(dto.AttendeeUserId, "RegistrationRequestReceived", "On-behalf registration filed",
                   $"An event manager filed an on-behalf registration for event #{dto.EventId}.", dto.EventId, null, created.RegistrationRequestId, ct);
               return Map(created);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "CreateAsync failed for on-behalf request event={EventId} attendee={AttendeeId}", dto.EventId, dto.AttendeeUserId);
               return null;
           }
       }

       public async Task<RegistrationRequestResponseDto?> DecideAsync(long callerUserId, long requestId, RegistrationRequestDecisionDto dto, string ipAddress, CancellationToken ct = default)
       {
           try
           {
               var req = await _requests.GetByIdAsync(requestId, ct);
               if (req is null) return null;
               if (req.RequestStatus != PendingStatus)
                   throw new InvalidOperationException($"Request is not pending (current: {req.RequestStatus}).");

               var decision = (dto.Decision ?? string.Empty).Trim();
               if (!string.Equals(decision, AcceptedStatus, StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(decision, RejectedStatus, StringComparison.OrdinalIgnoreCase))
                   throw new InvalidOperationException("Decision must be 'Accepted' or 'Rejected'.");

               var now = DateTime.UtcNow;
               req.RequestStatus = string.Equals(decision, AcceptedStatus, StringComparison.OrdinalIgnoreCase) ? AcceptedStatus : RejectedStatus;
               req.RespondedAtUtc = now;
               req.ResponseComment = dto.ResponseComment;

               if (req.RequestStatus == AcceptedStatus)
               {
                   var result = await _registrationRepo.RegisterAttendeeWithCapacityCheckAsync(req.EventId, req.AttendeeUserId, "Approved", ct);
                   if (result.Outcome == RegistrationOutcome.Confirmed)
                       req.LinkedRegistrationId = result.RegistrationId;
               }

               await _requests.UpdateAsync(req, ct);
               await _audit.LogAuditAsync(callerUserId,
                   req.RequestStatus == AcceptedStatus ? AuditActionTypes.RegistrationRequestAccepted : AuditActionTypes.RegistrationRequestRejected,
                   "RegistrationRequest", req.RegistrationRequestId, "Success", ipAddress,
                   dto.ResponseComment is null ? null : $"{{\"comment\":\"{dto.ResponseComment}\"}}", req.EventId);

               await SafeNotify(req.AttendeeUserId,
                   req.RequestStatus == AcceptedStatus ? "RegistrationRequestApproved" : "RegistrationRequestRejected",
                   $"On-behalf registration {req.RequestStatus.ToLowerInvariant()}",
                   $"Your on-behalf registration for event #{req.EventId} was {req.RequestStatus.ToLowerInvariant()}.",
                   req.EventId, req.LinkedRegistrationId, req.RegistrationRequestId, ct);

               return Map(req);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "DecideAsync failed for request {RequestId}.", requestId);
               return null;
           }
       }

       public async Task<PaginatedResponse<RegistrationRequestResponseDto>> ListByEventAsync(long eventId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default)
       {
           if (pageNumber < 1) pageNumber = 1;
           if (pageSize < 1) pageSize = 20;
           if (pageSize > 100) pageSize = 100;

           var list = await _requests.ListByEventAsync(eventId, statusFilter, pageNumber, pageSize, ct);
           var total = await _requests.CountByEventAsync(eventId, statusFilter, ct);
           return new PaginatedResponse<RegistrationRequestResponseDto>
           {
               Data = list.Select(Map).ToList(),
               PageNumber = pageNumber,
               PageSize = pageSize,
               TotalRecords = total,
               TotalPages = (int)Math.Ceiling(total / (double)pageSize)
           };
       }

       private static RegistrationRequestResponseDto Map(RegistrationRequest r) => new()
       {
           RegistrationRequestId = r.RegistrationRequestId,
           EventId = r.EventId,
           AttendeeUserId = r.AttendeeUserId,
           AttendeeDisplayName = r.AttendeeUser?.DisplayName ?? string.Empty,
           RequestedByUserId = r.RequestedByUserId,
           RequestType = r.RequestType,
           RequestStatus = r.RequestStatus,
           RequestedAtUtc = r.RequestedAtUtc,
           RespondedAtUtc = r.RespondedAtUtc,
           ResponseComment = r.ResponseComment,
           LinkedRegistrationId = r.LinkedRegistrationId
       };

       private async Task SafeNotify(long recipient, string type, string title, string message, long? eventId, long? registrationId, long? requestId, CancellationToken ct)
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
                   RelatedRegistrationId = registrationId,
                   RelatedRequestId = requestId,
                   DeliveryStatus = "Sent",
                   SentAtUtc = DateTime.UtcNow
               }, ct);
           }
           catch { /* best-effort */ }
       }
   }
}
