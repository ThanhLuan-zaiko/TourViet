namespace TourViet.ViewModels;

public class BookingViewModel
{
    // Booking Info
    public Guid BookingID { get; set; }
    public string BookingRef { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string StatusIcon { get; set; } = "bi-circle";
    public DateTime CreatedAt { get; set; }
    public string FormattedCreatedAt { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    
    // Customer Info
    public Guid UserID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string CustomerInitials { get; set; } = "U";
    
    // Tour Info
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? TourSlug { get; set; }
    public string TourCategory { get; set; } = string.Empty;
    public string? TourImageUrl { get; set; }
    public List<string> TourImages { get; set; } = new();
    
    // Instance Info
    public Guid InstanceID { get; set; }
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
    
    // Booking Details
    public int Seats { get; set; }
    public decimal BasePrice { get; set; }
    public decimal BasePriceTotal { get; set; }
    public decimal ServicesTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string FormattedTotalAmount { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    
    // Promotion Info
    public string? PromotionName { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? FormattedDiscountAmount { get; set; }
    public decimal SubTotalBeforeDiscount { get; set; }
    public string? FormattedSubTotalBeforeDiscount { get; set; }
    
    // Services
    public List<BookedServiceViewModel> BookedServices { get; set; } = new();
    public int ServicesCount { get; set; }
}

public class BookedServiceViewModel
{
    public Guid ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceAtBooking { get; set; }
    public decimal SubTotal { get; set; }
    public string Currency { get; set; } = "VND";
    public string FormattedSubTotal { get; set; } = string.Empty;
}
