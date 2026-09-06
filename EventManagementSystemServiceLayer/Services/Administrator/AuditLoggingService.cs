using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;

namespace EventManagementSystemServiceLayer.Services.Administrator
{
   public interface IAuditLoggingService
   {
       Task<AuditRecord?> LogAuditAsync(
           long actorUserId,
           string actionType,
           string targetEntity,
           long targetEntityId,
           string outcome,
           string ipAddress,
           string? metadataJson = null,
           long? eventId = null);
   }

   public class AuditLoggingService : IAuditLoggingService
   {
       private readonly IAuditRepository _auditRepository;

       public AuditLoggingService(IAuditRepository auditRepository)
       {
           _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
       }

       public async Task<AuditRecord?> LogAuditAsync(
           long actorUserId,
           string actionType,
           string targetEntity,
           long targetEntityId,
           string outcome,
           string ipAddress,
           string? metadataJson = null,
           long? eventId = null)
       {
           try
           {
               if (string.IsNullOrWhiteSpace(actionType))
                   throw new ArgumentException("Action type cannot be null or empty.", nameof(actionType));
               if (string.IsNullOrWhiteSpace(targetEntity))
                   throw new ArgumentException("Target entity cannot be null or empty.", nameof(targetEntity));
               if (string.IsNullOrWhiteSpace(outcome))
                   throw new ArgumentException("Outcome cannot be null or empty.", nameof(outcome));

               var auditRecord = new AuditRecord
               {
                   ActorUserId = actorUserId,
                   ActionType = actionType,
                   TargetEntity = targetEntity,
                   TargetEntityId = targetEntityId,
                   Outcome = outcome,
                   IpAddress = ipAddress ?? "Unknown",
                   MetadataJson = metadataJson,
                   EventId = eventId,
                   CreatedAtUtc = DateTime.UtcNow
               };

               return await _auditRepository.AddAuditRecordAsync(auditRecord);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in LogAuditAsync: {ex.Message}");
               throw;
           }
       }
   }
}
