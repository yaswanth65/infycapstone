using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
    public interface IEventApprovalRepository
    {
        Task<IReadOnlyList<EventApprovalRequest>> GetPendingApprovalsAsync(CancellationToken ct = default);
        Task<EventApprovalRequest?> GetByEventIdAsync(long eventId, CancellationToken ct = default);
        Task<EventApprovalRequest?> GetByIdAsync(long approvalRequestId, CancellationToken ct = default);
        Task<EventApprovalRequest> SubmitAsync(EventApprovalRequest request, CancellationToken ct = default);
        Task<EventApprovalRequest> ReviewAsync(EventApprovalRequest request, CancellationToken ct = default);
    }
}
