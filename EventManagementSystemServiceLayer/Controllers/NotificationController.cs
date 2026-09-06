using System.Security.Claims;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/notifications")]
   [Authorize]
   public class NotificationController : ControllerBase
   {
       private readonly INotificationService _notificationService;

       public NotificationController(INotificationService notificationService)
       {
           _notificationService = notificationService;
       }

       [HttpGet]
       public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
       {
           var result = await _notificationService.ListAsync(GetCurrentUserId(), unreadOnly, pageNumber, pageSize);
           return Ok(new ApiResponse<PaginatedResponse<NotificationItemDto>>(true, 200, "Notifications retrieved.", result));
       }

       [HttpGet("unread-count")]
       public async Task<IActionResult> UnreadCount()
       {
           var count = await _notificationService.UnreadCountAsync(GetCurrentUserId());
           return Ok(new ApiResponse<int>(true, 200, "Unread count retrieved.", count));
       }

       [HttpPut("{notificationId:long}/read")]
       public async Task<IActionResult> MarkAsRead(long notificationId)
       {
           var ok = await _notificationService.MarkAsReadAsync(GetCurrentUserId(), notificationId);
           if (!ok) return NotFound(new ApiResponse<object>(false, 404, $"Notification {notificationId} not found."));
           return Ok(new ApiResponse<object>(true, 200, "Notification marked as read."));
       }

       [HttpPut("read-all")]
       public async Task<IActionResult> MarkAllRead()
       {
           await _notificationService.MarkAllReadAsync(GetCurrentUserId());
           return Ok(new ApiResponse<object>(true, 200, "All notifications marked as read."));
       }

       private long GetCurrentUserId()
       {
           var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UserId");
           if (long.TryParse(claim, out var id)) return id;
           return 0;
       }
   }
}
