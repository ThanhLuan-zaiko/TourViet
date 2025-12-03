namespace TourViet.DTOs;

public class TourSearchResultDto
{
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? LocationName { get; set; }
    public string? City { get; set; }
    public string? CountryName { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? MinPrice { get; set; }
    public string Currency { get; set; } = "VND";
}
