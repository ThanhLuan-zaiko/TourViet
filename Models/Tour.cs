using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Tours")]
public class Tour
{
    [Key]
    public Guid TourID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string TourName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public Guid? LocationID { get; set; }

    public Guid? CategoryID { get; set; }

    public Guid? DefaultGuideID { get; set; }

    [Required]
    public bool IsPublished { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    [ForeignKey("LocationID")]
    public virtual Location? Location { get; set; }

    [ForeignKey("CategoryID")]
    public virtual Category? Category { get; set; }

    [ForeignKey("DefaultGuideID")]
    public virtual User? DefaultGuide { get; set; }

    public virtual ICollection<TourInstance> TourInstances { get; set; } = new List<TourInstance>();
    public virtual ICollection<TourPrice> TourPrices { get; set; } = new List<TourPrice>();
    public virtual ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();
    public virtual ICollection<TourImage> TourImages { get; set; } = new List<TourImage>();
    public virtual ICollection<TourService> TourServices { get; set; } = new List<TourService>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}