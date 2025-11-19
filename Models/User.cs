using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Users")]
public class User
{
    [Key]
    public Guid UserID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    [Required]
    [MaxLength(64)]
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    [Required]
    [MaxLength(100)]
    public string PasswordAlgo { get; set; } = "SHA2_512+iter1000";

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? FullName { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

