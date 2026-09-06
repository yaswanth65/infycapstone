using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public interface IAuditRepository
   {
       Task<AuditRecord?> GetAuditRecordByIdAsync(long auditRecordId);
       Task<List<AuditRecord>> GetFilteredAuditRecordsAsync(
           long? actorUserId = null,
           string? actionType = null,
           DateTime? startDate = null,
           DateTime? endDate = null,
           int pageNumber = 1,
           int pageSize = 10);
       Task<int> GetFilteredAuditRecordsCountAsync(
           long? actorUserId = null,
           string? actionType = null,
           DateTime? startDate = null,
           DateTime? endDate = null);
       Task<long> GetTotalAuditRecordCountAsync();
       Task<long> GetAuditRecordCountSinceAsync(DateTime sinceUtc);
       Task<AuditRecord> AddAuditRecordAsync(AuditRecord auditRecord);
       Task<int> SaveChangesAsync();
   }
}
