using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("UserRoles")]
public class UserRole
{
    [Key]
    [Column(Order = 0)]
    public Guid UserID { get; set; }

    [Key]
    [Column(Order = 1)]
    public Guid RoleID { get; set; }

    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedBy { get; set; }

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleID")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("AssignedBy")]
    public virtual User? AssignedByUser { get; set; }
}

