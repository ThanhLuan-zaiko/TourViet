using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;

namespace TourViet.Services;

public class TourInstanceService : ITourInstanceService
{
    private readonly TourBookingDbContext _context;

    public TourInstanceService(TourBookingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TourInstance>> GetAllTourInstancesAsync()
    {
        return await _context.TourInstances
            .Include(ti => ti.Tour)
            .Include(ti => ti.Guide)
            .ToListAsync();
    }

    public async Task<TourInstance?> GetTourInstanceByIdAsync(Guid id)
    {
        return await _context.TourInstances
            .Include(ti => ti.Tour)
            .Include(ti => ti.Guide)
            .Include(ti => ti.Bookings)
            .Include(ti => ti.TourPrices)
            .FirstOrDefaultAsync(ti => ti.InstanceID == id);
    }

    public async Task<TourInstance> CreateTourInstanceAsync(TourInstance tourInstance)
    {
        tourInstance.InstanceID = Guid.NewGuid();
        tourInstance.CreatedAt = DateTime.UtcNow;
        _context.TourInstances.Add(tourInstance);
        await _context.SaveChangesAsync();
        return tourInstance;
    }

    public async Task<TourInstance> UpdateTourInstanceAsync(TourInstance tourInstance)
    {
        var existingInstance = await _context.TourInstances.FirstOrDefaultAsync(ti => ti.InstanceID == tourInstance.InstanceID);
        if (existingInstance == null)
        {
            throw new ArgumentException($"Tour Instance with ID {tourInstance.InstanceID} not found");
        }

        existingInstance.TourID = tourInstance.TourID;
        existingInstance.StartDate = tourInstance.StartDate;
        existingInstance.EndDate = tourInstance.EndDate;
        existingInstance.Capacity = tourInstance.Capacity;
        existingInstance.SeatsBooked = tourInstance.SeatsBooked;
        existingInstance.SeatsHeld = tourInstance.SeatsHeld;
        existingInstance.HoldExpires = tourInstance.HoldExpires;
        existingInstance.Status = tourInstance.Status;
        existingInstance.PriceBase = tourInstance.PriceBase;
        existingInstance.Currency = tourInstance.Currency;
        existingInstance.GuideID = tourInstance.GuideID;
        existingInstance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingInstance;
    }

    public async Task<bool> DeleteTourInstanceAsync(Guid id)
    {
        var tourInstance = await _context.TourInstances.FirstOrDefaultAsync(ti => ti.InstanceID == id);
        if (tourInstance == null)
        {
            return false;
        }

        _context.TourInstances.Remove(tourInstance);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TourInstance>> GetTourInstancesByTourIdAsync(Guid tourId)
    {
        return await _context.TourInstances
            .Include(ti => ti.Tour)
            .Include(ti => ti.Guide)
            .Where(ti => ti.TourID == tourId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TourInstance>> GetActiveTourInstancesAsync()
    {
        return await _context.TourInstances
            .Include(ti => ti.Tour)
            .Include(ti => ti.Guide)
            .Where(ti => ti.Status == "Open" && ti.StartDate >= DateTime.UtcNow)
            .ToListAsync();
    }
}