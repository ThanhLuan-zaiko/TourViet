using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using TourViet.Models;

namespace TourViet.Models.DTOs
{
    /// <summary>
    /// DTO used for updating a Tour. Contains only the fields that can be edited via the UI.
    /// </summary>
    public class TourUpdateDto
    {
        public Guid TourID { get; set; }
        public string? TourName { get; set; }
        public string? Slug { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public Guid CategoryID { get; set; }
        public Guid DefaultGuideID { get; set; }
        public bool IsPublished { get; set; }
        // Location / Country fields (optional)
        public string? NewCountryName { get; set; }
        public string? NewLocationName { get; set; }
        public string? NewLocationCity { get; set; }
        public string? NewLocationAddress { get; set; }
        public decimal? NewLocationLatitude { get; set; }
        public decimal? NewLocationLongitude { get; set; }
        public string? NewLocationDescription { get; set; }
        // Service IDs to associate with the tour
        public List<Guid>? TourServiceIds { get; set; }
        // Image files uploaded for the tour
        public List<IFormFile>? TourImageFiles { get; set; }
        // Collections for related entities (optional)
        public List<Itinerary>? Itineraries { get; set; }
        public List<TourPrice>? TourPrices { get; set; }
        public List<TourInstance>? TourInstances { get; set; }
    }
}
