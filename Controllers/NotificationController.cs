using Microsoft.AspNetCore.Mvc;
using TourViet.Services.Interfaces;

namespace TourViet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get recent notifications for the current user
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 10)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để xem thông báo." });
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);

            return Ok(new
            {
                success = true,
                data = notifications.Select(n => new
                {
                    notificationId = n.NotificationID,
                    title = n.Title,
                    message = n.Message,
                    notificationType = n.NotificationType,
                    sentAt = n.SentAt,
                    isRead = n.IsRead,
                    payload = n.Payload
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications");
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tải thông báo." });
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập." });
            }

            var result = await _notificationService.MarkAsReadAsync(id);

            if (result)
            {
                return Ok(new { success = true, message = "Đã đánh dấu đọc." });
            }
            else
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông báo." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi." });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Ok(new { success = true, count = 0 });
            }

            var count = await _notificationService.GetUnreadCountAsync(userId);

            return Ok(new { success = true, count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unread count");
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi." });
        }
    }
}
