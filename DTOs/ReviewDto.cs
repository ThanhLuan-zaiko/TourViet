namespace TourViet.DTOs;

public class ReviewDto
{
    public Guid ReviewID { get; set; }
    public Guid TourID { get; set; }
    public Guid UserID { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // User information
    public string UserFullName { get; set; } = string.Empty;
    public string UserInitial { get; set; } = string.Empty;
}

public class CreateReviewRequest
{
    public Guid TourID { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}

public class UpdateReviewRequest
{
    public Guid ReviewID { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewStatsDto
{
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
}
