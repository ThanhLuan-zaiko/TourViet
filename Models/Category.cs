using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Categories")]
public class Category
{
    [Key]
    public Guid CategoryID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();
}