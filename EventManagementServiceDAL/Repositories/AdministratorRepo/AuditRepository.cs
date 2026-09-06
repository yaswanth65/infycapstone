using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public class AuditRepository : IAuditRepository
   {
       private readonly EventManagementDbContext _context;

       public AuditRepository(EventManagementDbContext context)
       {
           _context = context ?? throw new ArgumentNullException(nameof(context));
       }

       public async Task<AuditRecord?> GetAuditRecordByIdAsync(long auditRecordId)
       {
           try
           {
               return await _context.AuditRecords
                   .Include(a => a.ActorUser)
                   .Include(a => a.Event)
                   .FirstOrDefaultAsync(a => a.AuditRecordId == auditRecordId);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAuditRecordByIdAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<List<AuditRecord>> GetFilteredAuditRecordsAsync(
           long? actorUserId = null,
           string? actionType = null,
           DateTime? startDate = null,
           DateTime? endDate = null,
           int pageNumber = 1,
           int pageSize = 10)
       {
           try
           {
               if (pageNumber < 1) pageNumber = 1;
               if (pageSize < 1) pageSize = 10;

               var query = _context.AuditRecords.AsQueryable();

               if (actorUserId.HasValue)
                   query = query.Where(a => a.ActorUserId == actorUserId);
               if (!string.IsNullOrWhiteSpace(actionType))
                   query = query.Where(a => a.ActionType == actionType);
               if (startDate.HasValue)
                   query = query.Where(a => a.CreatedAtUtc >= startDate);
               if (endDate.HasValue)
                   query = query.Where(a => a.CreatedAtUtc <= endDate);

               return await query
                   .Include(a => a.ActorUser)
                   .Include(a => a.Event)
                   .OrderByDescending(a => a.CreatedAtUtc)
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetFilteredAuditRecordsAsync: {ex.Message}");
               return new List<AuditRecord>();
           }
       }

       public async Task<int> GetFilteredAuditRecordsCountAsync(
           long? actorUserId = null,
           string? actionType = null,
           DateTime? startDate = null,
           DateTime? endDate = null)
       {
           try
           {
               var query = _context.AuditRecords.AsQueryable();

               if (actorUserId.HasValue)
                   query = query.Where(a => a.ActorUserId == actorUserId);
               if (!string.IsNullOrWhiteSpace(actionType))
                   query = query.Where(a => a.ActionType == actionType);
               if (startDate.HasValue)
                   query = query.Where(a => a.CreatedAtUtc >= startDate);
               if (endDate.HasValue)
                   query = query.Where(a => a.CreatedAtUtc <= endDate);

               return await query.CountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetFilteredAuditRecordsCountAsync: {ex.Message}");
               return 0;
           }
       }

       public async Task<long> GetTotalAuditRecordCountAsync()
       {
           try
           {
               return await _context.AuditRecords.LongCountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetTotalAuditRecordCountAsync: {ex.Message}");
               return 0;
           }
       }

       public async Task<long> GetAuditRecordCountSinceAsync(DateTime sinceUtc)
       {
           try
           {
               return await _context.AuditRecords
                   .Where(a => a.CreatedAtUtc >= sinceUtc)
                   .LongCountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAuditRecordCountSinceAsync: {ex.Message}");
               return 0;
           }
       }

       public async Task<AuditRecord> AddAuditRecordAsync(AuditRecord auditRecord)
       {
           try
           {
               if (auditRecord == null)
                   throw new ArgumentNullException(nameof(auditRecord));

               _context.AuditRecords.Add(auditRecord);
               await SaveChangesAsync();
               return auditRecord;
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in AddAuditRecordAsync: {ex.Message}");
               throw;
           }
       }

       public async Task<int> SaveChangesAsync()
       {
           try
           {
               return await _context.SaveChangesAsync();
           }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Users\YASWANTH\AppData\Local\Temp\opencode\audit_error.log", $"{System.DateTime.UtcNow:O} {ex}\n\n");
                return -99;
            }
       }
   }
}
