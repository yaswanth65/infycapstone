using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Brownfield
{
    public interface IEventApprovalWorkflowService
    {
        Task<EventApprovalResponseDto> SubmitForApprovalAsync(long requestedByUserId, EventApprovalSubmitDto dto, CancellationToken ct = default);
        Task<IReadOnlyList<EventApprovalResponseDto>> GetPendingApprovalsAsync(CancellationToken ct = default);
        Task<EventApprovalResponseDto?> ReviewApprovalAsync(long reviewedByUserId, EventApprovalReviewDto dto, CancellationToken ct = default);
    }

    public sealed class EventApprovalWorkflowService : IEventApprovalWorkflowService
    {
        private readonly IEventApprovalRepository _approvalRepo;
        private readonly IEventRepository _eventRepo;
        private readonly INotificationRepository _notifications;
        private readonly ILogger<EventApprovalWorkflowService> _logger;

        public EventApprovalWorkflowService(
            IEventApprovalRepository approvalRepo,
            IEventRepository eventRepo,
            INotificationRepository notifications,
            ILogger<EventApprovalWorkflowService> logger)
        {
            _approvalRepo = approvalRepo;
            _eventRepo = eventRepo;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<EventApprovalResponseDto> SubmitForApprovalAsync(long requestedByUserId, EventApprovalSubmitDto dto, CancellationToken ct = default)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId, ct: ct);
            if (ev is null) throw new InvalidOperationException("Event not found.");

            ev.ApprovalStatus = "Submitted";
            await _eventRepo.UpdateAsync(ev, ct);

            var req = new EventApprovalRequest
            {
                EventId = dto.EventId,
                RequestedByUserId = requestedByUserId,
                Status = "Pending",
                Remarks = dto.Remarks,
                RequestedAtUtc = DateTime.UtcNow
            };

            var created = await _approvalRepo.SubmitAsync(req, ct);
            return new EventApprovalResponseDto(created.ApprovalRequestId, ev.EventId, ev.Title, requestedByUserId, "", "Pending", dto.Remarks, created.RequestedAtUtc, null);
        }

        public async Task<IReadOnlyList<EventApprovalResponseDto>> GetPendingApprovalsAsync(CancellationToken ct = default)
        {
            var list = await _approvalRepo.GetPendingApprovalsAsync(ct);
            return list.Select(r => new EventApprovalResponseDto(
                r.ApprovalRequestId,
                r.EventId,
                r.Event?.Title ?? "Unknown",
                r.RequestedByUserId,
                r.RequestedByUser?.UserName ?? "Unknown",
                r.Status,
                r.Remarks,
                r.RequestedAtUtc,
                r.ReviewedAtUtc
            )).ToList();
        }

        public async Task<EventApprovalResponseDto?> ReviewApprovalAsync(long reviewedByUserId, EventApprovalReviewDto dto, CancellationToken ct = default)
        {
            var req = await _approvalRepo.GetByIdAsync(dto.ApprovalRequestId, ct);
            if (req is null) return null;

            req.ReviewedByUserId = reviewedByUserId;
            req.ReviewedAtUtc = DateTime.UtcNow;
            req.Status = dto.Approve ? "Approved" : "Rejected";
            req.Remarks = dto.Remarks;

            if (req.Event != null)
            {
                req.Event.ApprovalStatus = req.Status;
                req.Event.RejectionReason = dto.Approve ? null : dto.Remarks;
                req.Event.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _approvalRepo.ReviewAsync(req, ct);

            var ev = req.Event ?? await _eventRepo.GetByIdAsync(req.EventId, ct: ct);
            if (ev != null)
            {
                ev.ApprovalStatus = req.Status;
                if (!dto.Approve)
                {
                    ev.RejectionReason = dto.Remarks;
                }
                else
                {
                    ev.RejectionReason = null;
                }
                await _eventRepo.UpdateAsync(ev, ct);

                // Notify organizer
                await _notifications.CreateAsync(new Notification
                {
                    RecipientUserId = req.RequestedByUserId,
                    RelatedEventId = ev.EventId,
                    NotificationType = dto.Approve ? "EventApproved" : "EventRejected",
                    Title = dto.Approve ? "Event Approved" : "Event Needs Revision",
                    Message = dto.Approve
                        ? $"Your event '{ev.Title}' was approved and can now be published."
                        : $"Your event '{ev.Title}' was rejected: {dto.Remarks}",
                    DeliveryStatus = "Sent",
                    SentAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                }, ct);
            }

            return new EventApprovalResponseDto(
                req.ApprovalRequestId,
                req.EventId,
                ev?.Title ?? "",
                req.RequestedByUserId,
                req.RequestedByUser?.UserName ?? "",
                req.Status,
                req.Remarks,
                req.RequestedAtUtc,
                req.ReviewedAtUtc
            );
        }
    }
}

