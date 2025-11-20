using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;

namespace TourViet.Services;

public class TourService : ITourService
{
    private readonly TourBookingDbContext _context;

    public TourService(TourBookingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tour>> GetAllToursAsync()
    {
        return await _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Include(t => t.DefaultGuide)
            .Where(t => !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<Tour?> GetTourByIdAsync(Guid id)
    {
        return await _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Include(t => t.DefaultGuide)
            .Include(t => t.TourInstances)
            .Include(t => t.TourPrices)
            .Include(t => t.Itineraries)
            .Include(t => t.TourImages)
            .Include(t => t.TourServices)
                .ThenInclude(ts => ts.Service)
            .Include(t => t.Reviews)
            .FirstOrDefaultAsync(t => t.TourID == id && !t.IsDeleted);
    }

    public async Task<Tour> CreateTourAsync(Tour tour)
    {
        tour.TourID = Guid.NewGuid();
        tour.CreatedAt = DateTime.UtcNow;
        _context.Tours.Add(tour);
        await _context.SaveChangesAsync();
        return tour;
    }

    public async Task<Tour> UpdateTourAsync(Tour tour)
    {
        var existingTour = await _context.Tours.FirstOrDefaultAsync(t => t.TourID == tour.TourID && !t.IsDeleted);
        if (existingTour == null)
        {
            throw new ArgumentException($"Tour with ID {tour.TourID} not found");
        }

        existingTour.TourName = tour.TourName;
        existingTour.Slug = tour.Slug;
        existingTour.ShortDescription = tour.ShortDescription;
        existingTour.Description = tour.Description;
        existingTour.LocationID = tour.LocationID;
        existingTour.CategoryID = tour.CategoryID;
        existingTour.DefaultGuideID = tour.DefaultGuideID;
        existingTour.IsPublished = tour.IsPublished;
        existingTour.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingTour;
    }

    public async Task<bool> DeleteTourAsync(Guid id)
    {
        var tour = await _context.Tours.FirstOrDefaultAsync(t => t.TourID == id && !t.IsDeleted);
        if (tour == null)
        {
            return false;
        }

        tour.IsDeleted = true;
        tour.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Tour>> GetPublishedToursAsync()
    {
        return await _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Include(t => t.DefaultGuide)
            .Where(t => t.IsPublished && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Tour>> GetToursByLocationAsync(Guid locationId)
    {
        return await _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Include(t => t.DefaultGuide)
            .Where(t => t.LocationID == locationId && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Tour>> GetToursByCategoryAsync(Guid categoryId)
    {
        return await _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Include(t => t.DefaultGuide)
            .Where(t => t.CategoryID == categoryId && !t.IsDeleted)
            .ToListAsync();
    }
}