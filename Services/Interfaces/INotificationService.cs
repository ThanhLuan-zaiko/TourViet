using TourViet.Models;

namespace TourViet.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Create a notification for a booking status change
    /// </summary>
    Task<Notification> CreateBookingNotificationAsync(Guid userId, Guid bookingId, string status, string bookingRef, string tourName);

    /// <summary>
    /// Get recent notifications for a user
    /// </summary>
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int limit = 10);

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId);

    /// <summary>
    /// Get count of unread notifications for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId);
}
