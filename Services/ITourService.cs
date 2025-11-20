using TourViet.Models;

namespace TourViet.Services;

public interface ITourService
{
    Task<IEnumerable<Tour>> GetAllToursAsync();
    Task<Tour?> GetTourByIdAsync(Guid id);
    Task<Tour> CreateTourAsync(Tour tour);
    Task<Tour> UpdateTourAsync(Tour tour);
    Task<bool> DeleteTourAsync(Guid id);
    Task<IEnumerable<Tour>> GetPublishedToursAsync();
    Task<IEnumerable<Tour>> GetToursByLocationAsync(Guid locationId);
    Task<IEnumerable<Tour>> GetToursByCategoryAsync(Guid categoryId);
}