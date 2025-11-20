using TourViet.Models;

namespace TourViet.Services;

public interface ITourInstanceService
{
    Task<IEnumerable<TourInstance>> GetAllTourInstancesAsync();
    Task<TourInstance?> GetTourInstanceByIdAsync(Guid id);
    Task<TourInstance> CreateTourInstanceAsync(TourInstance tourInstance);
    Task<TourInstance> UpdateTourInstanceAsync(TourInstance tourInstance);
    Task<bool> DeleteTourInstanceAsync(Guid id);
    Task<IEnumerable<TourInstance>> GetTourInstancesByTourIdAsync(Guid tourId);
    Task<IEnumerable<TourInstance>> GetActiveTourInstancesAsync();
}