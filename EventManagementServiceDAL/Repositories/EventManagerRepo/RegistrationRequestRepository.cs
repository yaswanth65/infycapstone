using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.EventManagerRepo
{
   public interface IRegistrationRequestRepository
   {
       Task<RegistrationRequest?> GetByIdAsync(long requestId, CancellationToken ct = default);
       Task<List<RegistrationRequest>> ListByEventAsync(long eventId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default);
       Task<int> CountByEventAsync(long eventId, string? statusFilter, CancellationToken ct = default);
       Task<RegistrationRequest> AddAsync(RegistrationRequest request, CancellationToken ct = default);
       Task<RegistrationRequest> UpdateAsync(RegistrationRequest request, CancellationToken ct = default);
       Task<bool> HasPendingAsync(long eventId, long attendeeUserId, CancellationToken ct = default);
   }

   public sealed class RegistrationRequestRepository : IRegistrationRequestRepository
   {
       private const string PendingStatus = "Pending";
       private readonly EventManagementDbContext _db;

       public RegistrationRequestRepository(EventManagementDbContext db)
       {
           _db = db ?? throw new ArgumentNullException(nameof(db));
       }

       public Task<RegistrationRequest?> GetByIdAsync(long requestId, CancellationToken ct = default)
           => _db.RegistrationRequests
               .Include(r => r.AttendeeUser)
               .Include(r => r.Event)
               .FirstOrDefaultAsync(r => r.RegistrationRequestId == requestId, ct);

       public async Task<List<RegistrationRequest>> ListByEventAsync(long eventId, string? statusFilter, int pageNumber, int pageSize, CancellationToken ct = default)
       {
           if (pageNumber < 1) pageNumber = 1;
           if (pageSize < 1) pageSize = 20;
           var q = _db.RegistrationRequests.AsNoTracking().Include(r => r.AttendeeUser).Where(r => r.EventId == eventId);
           if (!string.IsNullOrWhiteSpace(statusFilter)) q = q.Where(r => r.RequestStatus == statusFilter);
           return await q.OrderByDescending(r => r.RequestedAtUtc).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
       }

       public Task<int> CountByEventAsync(long eventId, string? statusFilter, CancellationToken ct = default)
       {
           var q = _db.RegistrationRequests.AsNoTracking().Where(r => r.EventId == eventId);
           if (!string.IsNullOrWhiteSpace(statusFilter)) q = q.Where(r => r.RequestStatus == statusFilter);
           return q.CountAsync(ct);
       }

       public async Task<RegistrationRequest> AddAsync(RegistrationRequest request, CancellationToken ct = default)
       {
           _db.RegistrationRequests.Add(request);
           await _db.SaveChangesAsync(ct);
           return request;
       }

       public async Task<RegistrationRequest> UpdateAsync(RegistrationRequest request, CancellationToken ct = default)
       {
           _db.RegistrationRequests.Update(request);
           await _db.SaveChangesAsync(ct);
           return request;
       }

       public Task<bool> HasPendingAsync(long eventId, long attendeeUserId, CancellationToken ct = default)
           => _db.RegistrationRequests.AnyAsync(r => r.EventId == eventId
                                                && r.AttendeeUserId == attendeeUserId
                                                && r.RequestStatus == PendingStatus, ct);
   }
}
