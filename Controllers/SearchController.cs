using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.DTOs;

namespace TourViet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly TourBookingDbContext _context;
    private readonly ILogger<SearchController> _logger;

    public SearchController(TourBookingDbContext context, ILogger<SearchController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Search tours by name, location, category
    /// </summary>
    [HttpGet("tours")]
    public async Task<IActionResult> SearchTours([FromQuery] string query, [FromQuery] int limit = 8)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new { success = true, data = new List<TourSearchResultDto>() });
            }

            var searchTerm = query.Trim().ToLower();

            var tours = await _context.Tours
                .Include(t => t.Location)
                    .ThenInclude(l => l!.Country)
                .Include(t => t.Category)
                .Include(t => t.TourImages)
                .Include(t => t.TourInstances)
                .Where(t => t.IsPublished && !t.IsDeleted)
                .Where(t =>
                    // Search in tour name
                    t.TourName.ToLower().Contains(searchTerm) ||
                    // Search in location
                    (t.Location != null && (
                        t.Location.LocationName.ToLower().Contains(searchTerm) ||
                        (t.Location.City != null && t.Location.City.ToLower().Contains(searchTerm)) ||
                        (t.Location.Country != null && t.Location.Country.CountryName.ToLower().Contains(searchTerm))
                    )) ||
                    // Search in category
                    (t.Category != null && t.Category.CategoryName.ToLower().Contains(searchTerm)) ||
                    // Search in short description
                    (t.ShortDescription != null && t.ShortDescription.ToLower().Contains(searchTerm))
                )
                .Select(t => new
                {
                    Tour = t,
                    // Calculate relevance score (higher = more relevant)
                    RelevanceScore = 
                        (t.TourName.ToLower() == searchTerm ? 1000 : 0) + // Exact match
                        (t.TourName.ToLower().StartsWith(searchTerm) ? 500 : 0) + // Starts with
                        (t.TourName.ToLower().Contains(searchTerm) ? 100 : 0) + // Contains
                        (t.Location != null && t.Location.LocationName.ToLower().Contains(searchTerm) ? 50 : 0) +
                        (t.Category != null && t.Category.CategoryName.ToLower().Contains(searchTerm) ? 30 : 0)
                })
                .OrderByDescending(x => x.RelevanceScore)
                .Take(limit)
                .ToListAsync();

            var results = tours.Select(x => new TourSearchResultDto
            {
                TourID = x.Tour.TourID,
                TourName = x.Tour.TourName,
                Slug = x.Tour.Slug,
                ShortDescription = x.Tour.ShortDescription,
                LocationName = x.Tour.Location?.LocationName,
                City = x.Tour.Location?.City,
                CountryName = x.Tour.Location?.Country?.CountryName,
                CategoryName = x.Tour.Category?.CategoryName,
                ImageUrl = x.Tour.TourImages
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Url)
                    .FirstOrDefault() ?? x.Tour.TourImages
                    .OrderBy(img => img.SortOrder)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                MinPrice = x.Tour.TourInstances
                    .Where(ti => ti.Status == "Open" && ti.StartDate > DateTime.UtcNow)
                    .Select(ti => (decimal?)ti.PriceBase)
                    .Min(),
                Currency = x.Tour.TourInstances
                    .Where(ti => ti.Status == "Open")
                    .Select(ti => ti.Currency)
                    .FirstOrDefault() ?? "VND"
            }).ToList();

            return Ok(new { success = true, data = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tours with query: {Query}", query);
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tìm kiếm." });
        }
    }
}
