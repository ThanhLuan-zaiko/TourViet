namespace TourViet.DTOs;

public class BookingDetailsDto
{
    public Guid BookingID { get; set; }
    public string BookingRef { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int Seats { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? SpecialRequests { get; set; }
    
    // Customer Info
    public Guid UserID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    
    // Tour Info
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? TourDescription { get; set; }
    public string? TourCategory { get; set; }
    public string? TourImageUrl { get; set; }
    public List<string> TourImages { get; set; } = new();
    
    // Instance Info
    public Guid InstanceID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationDays { get; set; }
    public decimal PriceBase { get; set; }
    public string? GuideName { get; set; }
    
    // Location Info
    public string? LocationName { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    
    // Services
    public List<BookedServiceDto> BookedServices { get; set; } = new();
    public decimal ServicesTotal { get; set; }
}

public class BookedServiceDto
{
    public Guid ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceAtBooking { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
}

public class PriceCalculationDto
{
    public decimal BasePrice { get; set; }
    public int Seats { get; set; }
    public decimal BasePriceTotal { get; set; }
    public List<ServicePriceDto> Services { get; set; } = new();
    public decimal ServicesTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "VND";
    
    public decimal DiscountAmount { get; set; }
    public Guid? AppliedPromotionId { get; set; }
    public Guid? AppliedCouponId { get; set; }
    public string? PromotionMessage { get; set; }
}

public class ServicePriceDto
{
    public Guid ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}
