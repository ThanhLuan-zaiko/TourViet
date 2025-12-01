namespace TourViet.ViewModels;

public class CustomerDetailsViewModel
{
    // Personal Information
    public Guid UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedCreatedAt { get; set; } = string.Empty;
    
    // Statistics
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public string FormattedTotalSpent { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public bool IsBanned { get; set; }
    
    // Related Data
    public List<BookingViewModel> Bookings { get; set; } = new();
    public List<CustomerReviewViewModel> Reviews { get; set; } = new();
    
    // UI Helpers
    public string CustomerInitials { get; set; } = string.Empty;
}

public class CustomerReviewViewModel
{
    public Guid ReviewID { get; set; }
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedCreatedAt { get; set; } = string.Empty;
    
    // Tour Details
    public string? TourImageUrl { get; set; }
    public string? LocationName { get; set; }
    public string? Country { get; set; }
}
