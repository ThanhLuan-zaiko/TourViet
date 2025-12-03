using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Services;

public class NotificationService : INotificationService
{
    private readonly TourBookingDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(TourBookingDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Notification> CreateBookingNotificationAsync(
        Guid userId, 
        Guid bookingId, 
        string status, 
        string bookingRef, 
        string tourName)
    {
        var (title, message) = GenerateNotificationContent(status, bookingRef, tourName);

        var payload = new
        {
            BookingID = bookingId,
            BookingRef = bookingRef,
            TourName = tourName,
            Status = status
        };

        var notification = new Notification
        {
            NotificationID = Guid.NewGuid(),
            UserID = userId,
            Title = title,
            Message = message,
            Payload = JsonSerializer.Serialize(payload),
            NotificationType = "Booking",
            Channel = "InApp",
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created notification {NotificationID} for user {UserID} - Booking {BookingRef} status: {Status}",
            notification.NotificationID, userId, bookingRef, status);

        return notification;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int limit = 10)
    {
        return await _context.Notifications
            .Where(n => n.UserID == userId && !n.IsDeleted)
            .OrderByDescending(n => n.SentAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserID == userId && !n.IsRead && !n.IsDeleted)
            .CountAsync();
    }

    private (string Title, string Message) GenerateNotificationContent(string status, string bookingRef, string tourName)
    {
        return status switch
        {
            "Confirmed" => (
                "Đặt tour đã được xác nhận",
                $"Đặt tour {tourName} đã được xác nhận! Mã đặt: {bookingRef}"
            ),
            "Rejected" => (
                "Đặt tour đã bị từ chối",
                $"Đặt tour {tourName} đã bị từ chối. Mã đặt: {bookingRef}"
            ),
            "Cancelled" => (
                "Đặt tour đã bị hủy",
                $"Đặt tour {tourName} đã bị hủy. Mã đặt: {bookingRef}"
            ),
            _ => (
                "Cập nhật trạng thái đặt tour",
                $"Trạng thái đặt tour {tourName} đã được cập nhật. Mã đặt: {bookingRef}"
            )
        };
    }
}
