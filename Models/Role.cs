using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Roles")]
public class Role
{
    [Key]
    public Guid RoleID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

