using System.ComponentModel.DataAnnotations;
using TourViet.Models;

namespace TourViet.Models.DTOs
{
    /// <summary>
    /// DTO for creating a new tour with all related entities.
    /// </summary>
    public class TourCreateDto
    {
        [Required(ErrorMessage = "Tên tour là bắt buộc")]
        [StringLength(200)]
        public string? TourName { get; set; }

        [StringLength(250)]
        public string? Slug { get; set; }

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        public Guid? CategoryID { get; set; }
        public Guid? LocationID { get; set; }
        public Guid? DefaultGuideID { get; set; }
        public bool IsPublished { get; set; }

        // New Location fields
        public string? NewLocationName { get; set; }
        public string? NewLocationCity { get; set; }
        public string? NewLocationAddress { get; set; }
        public decimal? NewLocationLatitude { get; set; }
        public decimal? NewLocationLongitude { get; set; }
        public string? NewLocationDescription { get; set; }
        public string? NewCountryName { get; set; }

        // Related entities
        public List<Guid>? TourServiceIds { get; set; }
        public List<Itinerary>? Itineraries { get; set; }
        public List<TourPrice>? TourPrices { get; set; }
        public List<TourInstance>? TourInstances { get; set; }
    }
}
