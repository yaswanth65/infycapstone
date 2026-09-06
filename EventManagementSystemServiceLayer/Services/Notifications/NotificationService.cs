using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementSystemServiceLayer.DTOs;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Notifications
{
   public sealed class NotificationItemDto
   {
       public long NotificationId { get; set; }
       public string NotificationType { get; set; } = null!;
       public string Title { get; set; } = null!;
       public string Message { get; set; } = null!;
       public long? RelatedEventId { get; set; }
       public long? RelatedRegistrationId { get; set; }
       public long? RelatedRequestId { get; set; }
       public string DeliveryStatus { get; set; } = null!;
       public DateTime? SentAtUtc { get; set; }
       public DateTime? ReadAtUtc { get; set; }
       public DateTime CreatedAtUtc { get; set; }
   }

   public interface INotificationService
   {
       Task<PaginatedResponse<NotificationItemDto>> ListAsync(long recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken ct = default);
       Task<int> UnreadCountAsync(long recipientUserId, CancellationToken ct = default);
       Task<bool> MarkAsReadAsync(long recipientUserId, long notificationId, CancellationToken ct = default);
       Task MarkAllReadAsync(long recipientUserId, CancellationToken ct = default);
   }

   public sealed class NotificationService : INotificationService
   {
       private readonly INotificationRepository _repo;
       private readonly ILogger<NotificationService> _logger;

       public NotificationService(INotificationRepository repo, ILogger<NotificationService> logger)
       {
           _repo = repo;
           _logger = logger;
       }

       public async Task<PaginatedResponse<NotificationItemDto>> ListAsync(long recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken ct = default)
       {
           var list = await _repo.GetByRecipientAsync(recipientUserId, unreadOnly, pageNumber, pageSize, ct);
           var total = await _repo.CountByRecipientAsync(recipientUserId, unreadOnly, ct);
           return new PaginatedResponse<NotificationItemDto>
           {
               Data = list.Select(n => new NotificationItemDto
               {
                   NotificationId = n.NotificationId,
                   NotificationType = n.NotificationType,
                   Title = n.Title,
                   Message = n.Message,
                   RelatedEventId = n.RelatedEventId,
                   RelatedRegistrationId = n.RelatedRegistrationId,
                   RelatedRequestId = n.RelatedRequestId,
                   DeliveryStatus = n.DeliveryStatus,
                   SentAtUtc = n.SentAtUtc,
                   ReadAtUtc = n.ReadAtUtc,
                   CreatedAtUtc = n.CreatedAtUtc
               }).ToList(),
               PageNumber = pageNumber < 1 ? 1 : pageNumber,
               PageSize = pageSize < 1 ? 20 : pageSize,
               TotalRecords = total,
               TotalPages = pageSize < 1 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
           };
       }

       public Task<int> UnreadCountAsync(long recipientUserId, CancellationToken ct = default)
           => _repo.CountByRecipientAsync(recipientUserId, unreadOnly: true, ct);

       public async Task<bool> MarkAsReadAsync(long recipientUserId, long notificationId, CancellationToken ct = default)
       {
           var n = await _repo.GetByIdAsync(notificationId, ct);
           if (n is null || n.RecipientUserId != recipientUserId) return false;
           if (n.ReadAtUtc is not null) return true;
           await _repo.MarkAsReadAsync(n, ct);
           return true;
       }

       public Task MarkAllReadAsync(long recipientUserId, CancellationToken ct = default)
           => _repo.MarkAllReadAsync(recipientUserId, ct);
   }
}
