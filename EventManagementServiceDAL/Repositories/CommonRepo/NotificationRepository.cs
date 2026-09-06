using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.CommonRepo;

public interface INotificationRepository
{
   Task CreateAsync(Notification notification, CancellationToken cancellationToken = default);
   Task<List<Notification>> GetByRecipientAsync(long recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
   Task<int> CountByRecipientAsync(long recipientUserId, bool unreadOnly, CancellationToken cancellationToken = default);
   Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken = default);
   Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default);
   Task MarkAllReadAsync(long recipientUserId, CancellationToken cancellationToken = default);
}

public sealed class NotificationRepository : INotificationRepository
{
   private readonly EventManagementDbContext _db;
   private readonly ILogger<NotificationRepository> _logger;

   public NotificationRepository(EventManagementDbContext db, ILogger<NotificationRepository> logger)
   {
       _db = db;
       _logger = logger;
   }

   public async Task CreateAsync(Notification notification, CancellationToken cancellationToken = default)
   {
       try
       {
           notification.CreatedAtUtc = DateTime.UtcNow;
           if (string.IsNullOrEmpty(notification.DeliveryStatus))
           {
               notification.DeliveryStatus = "Sent";
               notification.SentAtUtc = DateTime.UtcNow;
           }
           _db.Notifications.Add(notification);
           await _db.SaveChangesAsync(cancellationToken);
       }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}.", notification.RecipientUserId);
            throw;
        }
    }

    public async Task<List<Notification>> GetByRecipientAsync(long recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == recipientUserId);
            if (unreadOnly)
                query = query.Where(n => n.ReadAtUtc == null);

            return await query
                .OrderByDescending(n => n.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}.", recipientUserId);
            return new List<Notification>();
        }
    }

    public async Task<int> CountByRecipientAsync(long recipientUserId, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == recipientUserId);
            if (unreadOnly)
                query = query.Where(n => n.ReadAtUtc == null);
            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count notifications for user {UserId}.", recipientUserId);
            return 0;
        }
    }

    public async Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification {Id}.", notificationId);
            return null;
        }
    }

    public async Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            notification.ReadAtUtc = DateTime.UtcNow;
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {Id} as read.", notification.NotificationId);
            throw;
        }
    }

    public async Task MarkAllReadAsync(long recipientUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var items = await _db.Notifications
                .Where(n => n.RecipientUserId == recipientUserId && n.ReadAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var n in items)
                n.ReadAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications read for user {UserId}.", recipientUserId);
            throw;
        }
    }
}
