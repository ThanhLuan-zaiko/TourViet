using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

public class Notification
{
    [Key]
    public Guid NotificationID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserID { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? Payload { get; set; }

    [MaxLength(50)]
    public string? NotificationType { get; set; }

    [MaxLength(50)]
    public string? Channel { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User? User { get; set; }
}
