using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    public Guid ReviewID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TourID { get; set; }

    [Required]
    public Guid UserID { get; set; }

    [Required]
    [Range(1, 5)]
    public byte Rating { get; set; }

    public string? Comment { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TourID")]
    public virtual Tour Tour { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}