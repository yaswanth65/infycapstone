using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
   public interface IEventRepository
   {
       Task<Event?> GetByIdAsync(long eventId, bool includeCounts = false, CancellationToken ct = default);
       Task<List<Event>> GetByOrganizerAsync(long organizerUserId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default);
       Task<int> GetByOrganizerCountAsync(long organizerUserId, string? statusFilter, CancellationToken ct = default);
       Task<Event> AddAsync(Event newEvent, CancellationToken ct = default);
       Task<Event> UpdateAsync(Event existingEvent, CancellationToken ct = default);
       Task AddStatusHistoryAsync(EventStatusHistory history, CancellationToken ct = default);
       Task<int> GetConfirmedRegistrationCountAsync(long eventId, CancellationToken ct = default);
       Task<int> GetWaitlistCountAsync(long eventId, CancellationToken ct = default);
   }

   public sealed class EventRepository : IEventRepository
   {
       private const string ConfirmedStatus = "Confirmed";
       private const string WaitingStatus = "Waiting";

       private readonly EventManagementDbContext _db;

       public EventRepository(EventManagementDbContext db)
       {
           _db = db ?? throw new ArgumentNullException(nameof(db));
       }

       public async Task<Event?> GetByIdAsync(long eventId, bool includeCounts = false, CancellationToken ct = default)
       {
           try
           {
               return await _db.Events
                   .Include(e => e.OrganizerUser)
                   .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in EventRepository.GetByIdAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<List<Event>> GetByOrganizerAsync(long organizerUserId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default)
       {
           try
           {
               if (pageNumber < 1) pageNumber = 1;
               if (pageSize < 1) pageSize = 20;

               var query = _db.Events.AsNoTracking().Where(e => e.OrganizerUserId == organizerUserId);
               if (!string.IsNullOrWhiteSpace(statusFilter))
                   query = query.Where(e => e.Status == statusFilter);

               return await query
                   .OrderByDescending(e => e.StartAtUtc)
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync(ct);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetByOrganizerAsync: {ex.Message}");
               return new List<Event>();
           }
       }

       public async Task<int> GetByOrganizerCountAsync(long organizerUserId, string? statusFilter, CancellationToken ct = default)
       {
           try
           {
               var query = _db.Events.AsNoTracking().Where(e => e.OrganizerUserId == organizerUserId);
               if (!string.IsNullOrWhiteSpace(statusFilter))
                   query = query.Where(e => e.Status == statusFilter);
               return await query.CountAsync(ct);
           }
           catch { return 0; }
       }

       public async Task<Event> AddAsync(Event newEvent, CancellationToken ct = default)
       {
           _db.Events.Add(newEvent);
           await _db.SaveChangesAsync(ct);
           return newEvent;
       }

       public async Task<Event> UpdateAsync(Event existingEvent, CancellationToken ct = default)
       {
           existingEvent.UpdatedAtUtc = DateTime.UtcNow;
           _db.Events.Update(existingEvent);
           await _db.SaveChangesAsync(ct);
           return existingEvent;
       }

       public async Task AddStatusHistoryAsync(EventStatusHistory history, CancellationToken ct = default)
       {
           _db.EventStatusHistories.Add(history);
           await _db.SaveChangesAsync(ct);
       }

       public Task<int> GetConfirmedRegistrationCountAsync(long eventId, CancellationToken ct = default)
           => _db.Registrations.CountAsync(r => r.EventId == eventId && r.RegistrationStatus == ConfirmedStatus, ct);

       public Task<int> GetWaitlistCountAsync(long eventId, CancellationToken ct = default)
           => _db.WaitlistEntries.CountAsync(w => w.EventId == eventId && w.WaitlistStatus == WaitingStatus, ct);
   }
}
