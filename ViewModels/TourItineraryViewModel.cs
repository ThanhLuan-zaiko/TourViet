namespace TourViet.ViewModels;

public class TourItineraryViewModel
{
    // Booking Info
    public Guid BookingID { get; set; }
    public string BookingRef { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string StatusIcon { get; set; } = "bi-circle";
    public DateTime CreatedAt { get; set; }
    public string FormattedCreatedAt { get; set; } = string.Empty;
    
    // Customer Info
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    
    // Tour Info
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string TourCategory { get; set; } = string.Empty;
    public string? TourImageUrl { get; set; }
    public List<string> TourImages { get; set; } = new();
    
    // Instance Info
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string FormattedStartDate { get; set; } = string.Empty;
    public string FormattedEndDate { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public string? GuideName { get; set; }
    
    // Location Info
    public string? LocationName { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? DestinationLat { get; set; }
    public decimal? DestinationLng { get; set; }
    public string? DestinationAddress { get; set; }
    
    // Booking Details
    public int Seats { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string FormattedTotalAmount { get; set; } = string.Empty;
    
    // Itinerary Details
    public List<ItineraryItemViewModel> Itineraries { get; set; } = new();
}

public class ItineraryItemViewModel
{
    public Guid ItineraryID { get; set; }
    public int DayIndex { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string DayLabel => $"Ngày {DayIndex}";
}
