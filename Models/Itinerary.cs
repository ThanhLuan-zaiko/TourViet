using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Itineraries")]
public class Itinerary
{
    [Key]
    public Guid ItineraryID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TourID { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int DayIndex { get; set; }

    [MaxLength(250)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    // Navigation properties
    [ForeignKey("TourID")]
    public virtual Tour Tour { get; set; } = null!;
}