using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
    public sealed class EventApprovalRepository : IEventApprovalRepository
    {
        private readonly EventManagementDbContext _db;
        private readonly ILogger<EventApprovalRepository> _logger;

        public EventApprovalRepository(EventManagementDbContext db, ILogger<EventApprovalRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<EventApprovalRequest>> GetPendingApprovalsAsync(CancellationToken ct = default)
        {
            return await _db.EventApprovalRequests
                .AsNoTracking()
                .Include(r => r.Event)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<EventApprovalRequest?> GetByEventIdAsync(long eventId, CancellationToken ct = default)
        {
            return await _db.EventApprovalRequests
                .Include(r => r.Event)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ReviewedByUser)
                .OrderByDescending(r => r.RequestedAtUtc)
                .FirstOrDefaultAsync(r => r.EventId == eventId, ct);
        }

        public async Task<EventApprovalRequest?> GetByIdAsync(long approvalRequestId, CancellationToken ct = default)
        {
            return await _db.EventApprovalRequests
                .Include(r => r.Event)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ReviewedByUser)
                .FirstOrDefaultAsync(r => r.ApprovalRequestId == approvalRequestId, ct);
        }

        public async Task<EventApprovalRequest> SubmitAsync(EventApprovalRequest request, CancellationToken ct = default)
        {
            request.RequestedAtUtc = DateTime.UtcNow;
            request.Status = "Pending";
            _db.EventApprovalRequests.Add(request);
            await _db.SaveChangesAsync(ct);
            return request;
        }

        public async Task<EventApprovalRequest> ReviewAsync(EventApprovalRequest request, CancellationToken ct = default)
        {
            request.ReviewedAtUtc = DateTime.UtcNow;
            _db.EventApprovalRequests.Update(request);
            await _db.SaveChangesAsync(ct);
            return request;
        }
    }
}
