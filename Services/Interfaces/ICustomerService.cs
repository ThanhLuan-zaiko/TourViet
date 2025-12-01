using TourViet.ViewModels;

namespace TourViet.Services.Interfaces;

public interface ICustomerService
{
    /// <summary>
    /// Get paginated list of customers with their statistics
    /// </summary>
    Task<List<CustomerListViewModel>> GetCustomersPagedAsync(int page = 1, int pageSize = 20);
    
    /// <summary>
    /// Get detailed information about a specific customer
    /// </summary>
    Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(Guid userId);
    
    /// <summary>
    /// Get total count of customers for pagination
    /// </summary>
    Task<int> GetCustomersCountAsync();
    
    /// <summary>
    /// Ban a customer account
    /// </summary>
    Task<(bool Success, string Message)> BanCustomerAsync(Guid userId);
    
    /// <summary>
    /// Unban a customer account
    /// </summary>
    Task<(bool Success, string Message)> UnbanCustomerAsync(Guid userId);
}
