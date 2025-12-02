namespace TourViet.DTOs;

public class CreateBookingDto
{
    public Guid TourInstanceID { get; set; }
    public int Seats { get; set; }
    public List<SelectedServiceDto> SelectedServices { get; set; } = new();
    public string? CouponCode { get; set; }
    
    // Customer contact info (optional, can use session user info)
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? SpecialRequests { get; set; }
}

public class SelectedServiceDto
{
    public Guid ServiceID { get; set; }
    public int Quantity { get; set; } = 1;
}
