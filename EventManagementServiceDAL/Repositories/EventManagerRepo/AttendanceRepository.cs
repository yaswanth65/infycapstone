using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
   public interface IAttendanceRepository
   {
       Task<AttendanceRecord?> GetByRegistrationIdAsync(long registrationId, CancellationToken ct = default);
       Task<List<AttendanceRecord>> GetByEventIdAsync(long eventId, CancellationToken ct = default);
       Task<AttendanceRecord> AddAsync(AttendanceRecord record, CancellationToken ct = default);
       Task<AttendanceRecord> UpdateAsync(AttendanceRecord record, CancellationToken ct = default);
       Task<Registration?> GetRegistrationAsync(long registrationId, CancellationToken ct = default);
   }

   public sealed class AttendanceRepository : IAttendanceRepository
   {
       private readonly EventManagementDbContext _db;

       public AttendanceRepository(EventManagementDbContext db)
       {
           _db = db ?? throw new ArgumentNullException(nameof(db));
       }

       public async Task<AttendanceRecord?> GetByRegistrationIdAsync(long registrationId, CancellationToken ct = default)
       {
           try
           {
               return await _db.AttendanceRecords
                   .Include(a => a.Registration).ThenInclude(r => r.Event)
                   .FirstOrDefaultAsync(a => a.RegistrationId == registrationId, ct);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetByRegistrationIdAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<List<AttendanceRecord>> GetByEventIdAsync(long eventId, CancellationToken ct = default)
       {
           try
           {
               return await _db.AttendanceRecords.AsNoTracking()
                   .Include(a => a.Registration).ThenInclude(r => r.AttendeeUser)
                   .Where(a => a.Registration.EventId == eventId)
                   .OrderByDescending(a => a.RecordedAtUtc)
                   .ToListAsync(ct);
           }
           catch { return new List<AttendanceRecord>(); }
       }

       public async Task<AttendanceRecord> AddAsync(AttendanceRecord record, CancellationToken ct = default)
       {
           _db.AttendanceRecords.Add(record);
           await _db.SaveChangesAsync(ct);
           return record;
       }

       public async Task<AttendanceRecord> UpdateAsync(AttendanceRecord record, CancellationToken ct = default)
       {
           _db.AttendanceRecords.Update(record);
           await _db.SaveChangesAsync(ct);
           return record;
       }

       public Task<Registration?> GetRegistrationAsync(long registrationId, CancellationToken ct = default)
           => _db.Registrations.Include(r => r.Event).FirstOrDefaultAsync(r => r.RegistrationId == registrationId, ct);
   }
}
