namespace TourViet.DTOs;

/// <summary>
/// Data Transfer Object for Tour entities used in SignalR real-time updates.
/// Contains only essential properties to minimize payload size.
/// </summary>
public class TourDto
{
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinPrice { get; set; }
    public string? MainImageUrl { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsDomestic { get; set; }
    public Guid? CountryId { get; set; }
    public DateTime CreatedAt { get; set; }
}
