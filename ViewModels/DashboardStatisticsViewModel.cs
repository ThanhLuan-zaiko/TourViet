namespace TourViet.ViewModels;

public class DashboardStatisticsViewModel
{
    // Overall Statistics
    public int TotalTours { get; set; }
    public int TotalBookings { get; set; }
    public int TotalCustomers { get; set; }
    public int UpcomingTours { get; set; }
    
    // Revenue Statistics
    public decimal TotalRevenue { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public string Currency { get; set; } = "VND";
    
    // Booking Status Distribution
    public Dictionary<string, int> BookingsByStatus { get; set; } = new();
    
    // Top Tours
    public List<TopTourViewModel> TopTours { get; set; } = new();
    
    // Monthly Revenue Trends (Last 12 months)
    public List<MonthlyDataViewModel> MonthlyRevenue { get; set; } = new();
    
    // Monthly Bookings Trends
    public List<MonthlyDataViewModel> MonthlyBookings { get; set; } = new();
    
    // Customer Growth (Last 12 months)
    public List<MonthlyDataViewModel> CustomerGrowth { get; set; } = new();
    
    // Category Distribution
    public Dictionary<string, int> ToursByCategory { get; set; } = new();
    
    // Recent Bookings Trend (Last 30 days)
    public List<DailyDataViewModel> DailyBookings { get; set; } = new();
}

public class TopTourViewModel
{
    public string TourName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MonthlyDataViewModel
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class DailyDataViewModel
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
