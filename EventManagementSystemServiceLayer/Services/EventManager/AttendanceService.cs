using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.Constants;
using EventManagementSystemServiceLayer.DTOs.EventManager;
using EventManagementSystemServiceLayer.Services.Administrator;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.EventManager
{
   public interface IAttendanceService
   {
       Task<AttendanceResponseDto?> RecordAsync(long callerUserId, AttendanceRecordDto dto, string ipAddress, CancellationToken ct = default);
       Task<AttendanceResponseDto?> CorrectAsync(long callerUserId, long registrationId, AttendanceCorrectionDto dto, string ipAddress, CancellationToken ct = default);
       Task<AttendanceResponseDto?> GetForRegistrationAsync(long registrationId, CancellationToken ct = default);
       Task<List<AttendanceResponseDto>> GetForEventAsync(long eventId, CancellationToken ct = default);
   }

   public sealed class AttendanceService : IAttendanceService
   {
       private readonly IAttendanceRepository _attendance;
       private readonly INotificationRepository _notifications;
       private readonly IAuditLoggingService _audit;
       private readonly ILogger<AttendanceService> _logger;

       public AttendanceService(IAttendanceRepository attendance, INotificationRepository notifications, IAuditLoggingService audit, ILogger<AttendanceService> logger)
       {
           _attendance = attendance;
           _notifications = notifications;
           _audit = audit;
           _logger = logger;
       }

       public async Task<AttendanceResponseDto?> RecordAsync(long callerUserId, AttendanceRecordDto dto, string ipAddress, CancellationToken ct = default)
       {
           try
           {
               var reg = await _attendance.GetRegistrationAsync(dto.RegistrationId, ct);
               if (reg is null) throw new InvalidOperationException("Registration not found.");
               if (reg.RegistrationStatus != "Confirmed") throw new InvalidOperationException("Attendance can only be recorded for confirmed registrations.");

               var existing = await _attendance.GetByRegistrationIdAsync(dto.RegistrationId, ct);
               var now = DateTime.UtcNow;
               AttendanceRecord record;
               if (existing is null)
               {
                   record = new AttendanceRecord
                   {
                       RegistrationId = dto.RegistrationId,
                       AttendanceStatus = dto.AttendanceStatus,
                       RecordedByUserId = callerUserId,
                       RecordedAtUtc = now,
                       IsFinalized = dto.Finalize,
                       FinalizedAtUtc = dto.Finalize ? now : null,
                       RevisionNo = 1
                   };
                   record = await _attendance.AddAsync(record, ct);
               }
               else
               {
                   if (existing.IsFinalized) throw new InvalidOperationException("Attendance is finalized; use correction endpoint.");
                   existing.AttendanceStatus = dto.AttendanceStatus;
                   existing.RecordedByUserId = callerUserId;
                   existing.RecordedAtUtc = now;
                   existing.IsFinalized = dto.Finalize;
                   existing.FinalizedAtUtc = dto.Finalize ? now : existing.FinalizedAtUtc;
                   record = await _attendance.UpdateAsync(existing, ct);
               }

               await _audit.LogAuditAsync(callerUserId, AuditActionTypes.AttendanceRecorded, "AttendanceRecord", record.AttendanceRecordId, "Success", ipAddress, $"{{\"status\":\"{record.AttendanceStatus}\"}}", reg.EventId);
               await SafeNotify(reg.AttendeeUserId, "AttendanceRecorded", "Attendance Recorded", $"Your attendance for event #{reg.EventId} was recorded as {record.AttendanceStatus}.", reg.EventId, dto.RegistrationId, ct);
               return Map(record, reg.EventId);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "RecordAsync failed for registration {RegId}.", dto.RegistrationId);
               return null;
           }
       }

       public async Task<AttendanceResponseDto?> CorrectAsync(long callerUserId, long registrationId, AttendanceCorrectionDto dto, string ipAddress, CancellationToken ct = default)
       {
           try
           {
               var existing = await _attendance.GetByRegistrationIdAsync(registrationId, ct);
               if (existing is null) throw new InvalidOperationException("Attendance record not found.");

               var now = DateTime.UtcNow;
               existing.AttendanceStatus = dto.AttendanceStatus;
               existing.CorrectedByUserId = callerUserId;
               existing.CorrectedAtUtc = now;
               existing.CorrectionReason = dto.CorrectionReason;
               existing.RevisionNo += 1;
               var updated = await _attendance.UpdateAsync(existing, ct);
               var eventId = existing.Registration?.EventId ?? 0;
               await _audit.LogAuditAsync(callerUserId, AuditActionTypes.AttendanceCorrected, "AttendanceRecord", updated.AttendanceRecordId, "Success", ipAddress, $"{{\"reason\":\"{dto.CorrectionReason}\"}}", eventId);
               return Map(updated, eventId);
           }
           catch (InvalidOperationException) { throw; }
           catch (Exception ex)
           {
               _logger.LogError(ex, "CorrectAsync failed for registration {RegId}.", registrationId);
               return null;
           }
       }

       public async Task<AttendanceResponseDto?> GetForRegistrationAsync(long registrationId, CancellationToken ct = default)
       {
           var rec = await _attendance.GetByRegistrationIdAsync(registrationId, ct);
           return rec is null ? null : Map(rec, rec.Registration?.EventId ?? 0);
       }

       public async Task<List<AttendanceResponseDto>> GetForEventAsync(long eventId, CancellationToken ct = default)
       {
           var list = await _attendance.GetByEventIdAsync(eventId, ct);
           return list.Select(a => Map(a, eventId)).ToList();
       }

       private static AttendanceResponseDto Map(AttendanceRecord a, long eventId) => new()
       {
           AttendanceRecordId = a.AttendanceRecordId,
           RegistrationId = a.RegistrationId,
           EventId = eventId,
           AttendanceStatus = a.AttendanceStatus,
           RecordedByUserId = a.RecordedByUserId,
           RecordedAtUtc = a.RecordedAtUtc,
           IsFinalized = a.IsFinalized,
           FinalizedAtUtc = a.FinalizedAtUtc,
           CorrectedByUserId = a.CorrectedByUserId,
           CorrectedAtUtc = a.CorrectedAtUtc,
           CorrectionReason = a.CorrectionReason,
           RevisionNo = a.RevisionNo
       };

       private async Task SafeNotify(long recipient, string type, string title, string message, long? eventId, long? registrationId, CancellationToken ct)
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
                   DeliveryStatus = "Sent",
                   SentAtUtc = DateTime.UtcNow
               }, ct);
           }
           catch { /* best-effort */ }
       }
   }
}
