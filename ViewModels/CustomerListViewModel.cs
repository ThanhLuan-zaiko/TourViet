namespace TourViet.ViewModels;

public class CustomerListViewModel
{
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
    
    // UI Helpers
    public string CustomerInitials { get; set; } = string.Empty;
}
