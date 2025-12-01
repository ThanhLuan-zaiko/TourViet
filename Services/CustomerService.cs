using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Services.Interfaces;
using TourViet.ViewModels;

namespace TourViet.Services;

public class CustomerService : ICustomerService
{
    private readonly TourBookingDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(TourBookingDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CustomerListViewModel>> GetCustomersPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            var customers = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Customer"))
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<CustomerListViewModel>();

            foreach (var customer in customers)
            {
                // Get booking statistics
                var bookings = await _context.Bookings
                    .Where(b => b.UserID == customer.UserID)
                    .ToListAsync();

                var totalBookings = bookings.Count;
                var totalSpent = bookings
                    .Where(b => b.Status != "Cancelled" && b.Status != "Rejected")
                    .Sum(b => b.TotalAmount);

                // Get review statistics
                var reviews = await _context.Reviews
                    .Where(r => r.UserID == customer.UserID)
                    .ToListAsync();

                var totalReviews = reviews.Count;
                var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                var initials = string.IsNullOrEmpty(customer.FullName) 
                    ? customer.Username.Substring(0, Math.Min(2, customer.Username.Length)).ToUpper()
                    : customer.FullName.Substring(0, 1).ToUpper();

                result.Add(new CustomerListViewModel
                {
                    UserID = customer.UserID,
                    Username = customer.Username,
                    FullName = customer.FullName ?? customer.Username,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    CreatedAt = customer.CreatedAt,
                    FormattedCreatedAt = customer.CreatedAt.ToString("dd/MM/yyyy"),
                    TotalBookings = totalBookings,
                    TotalSpent = totalSpent,
                    FormattedTotalSpent = $"{totalSpent.ToString("N0")} VND",
                    TotalReviews = totalReviews,
                    AverageRating = averageRating,
                    IsBanned = customer.IsDeleted,
                    CustomerInitials = initials
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers list");
            return new List<CustomerListViewModel>();
        }
    }

    public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(Guid userId)
    {
        try
        {
            var customer = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId && !u.IsDeleted);

            if (customer == null)
                return null;

            // Get all bookings for this customer
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TourInstance)
                    .ThenInclude(ti => ti.Tour)
                        .ThenInclude(t => t.Category)
                .Include(b => b.TourInstance)
                    .ThenInclude(ti => ti.Tour)
                        .ThenInclude(t => t.TourImages)
                .Include(b => b.TourInstance)
                    .ThenInclude(ti => ti.Tour)
                        .ThenInclude(t => t.Location)
                            .ThenInclude(l => l!.Country)
                .Include(b => b.TourInstance)
                    .ThenInclude(ti => ti.Guide)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var bookingViewModels = new List<BookingViewModel>();
            
            foreach (var booking in bookings)
            {
                // Get booked services for each booking
                var bookedServices = await _context.BookingServices
                    .Include(bs => bs.Service)
                    .Where(bs => bs.BookingID == booking.BookingID)
                    .Select(bs => new BookedServiceViewModel
                    {
                        ServiceID = bs.ServiceID,
                        ServiceName = bs.Service.ServiceName,
                        Quantity = bs.Quantity,
                        PriceAtBooking = bs.PriceAtBooking,
                        SubTotal = bs.PriceAtBooking * bs.Quantity,
                        Currency = bs.Currency,
                        FormattedSubTotal = $"{(bs.PriceAtBooking * bs.Quantity).ToString("N0")} {bs.Currency}"
                    })
                    .ToListAsync();

                var servicesTotal = bookedServices.Sum(s => s.SubTotal);

                bookingViewModels.Add(new BookingViewModel
                {
                    BookingID = booking.BookingID,
                    BookingRef = booking.BookingRef,
                    Status = booking.Status,
                    StatusBadgeClass = GetStatusBadgeClass(booking.Status),
                    StatusIcon = GetStatusIcon(booking.Status),
                    CreatedAt = booking.CreatedAt,
                    FormattedCreatedAt = booking.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    
                    UserID = booking.UserID,
                    CustomerName = customer.FullName ?? customer.Username,
                    CustomerEmail = customer.Email,
                    CustomerPhone = customer.Phone,
                    CustomerAddress = customer.Address,
                    CustomerInitials = string.IsNullOrEmpty(customer.FullName) ? "U" : customer.FullName.Substring(0, 1).ToUpper(),
                    
                    TourID = booking.TourInstance.TourID,
                    TourName = booking.TourInstance.Tour.TourName,
                    TourCategory = booking.TourInstance.Tour.Category?.CategoryName ?? "N/A",
                    TourImageUrl = booking.TourInstance.Tour.TourImages?.FirstOrDefault(i => i.IsPrimary)?.Url ?? 
                                   booking.TourInstance.Tour.TourImages?.FirstOrDefault()?.Url,
                    TourImages = booking.TourInstance.Tour.TourImages?.Select(i => i.Url).ToList() ?? new List<string>(),
                    
                    InstanceID = booking.InstanceID,
                    StartDate = booking.TourInstance.StartDate,
                    EndDate = booking.TourInstance.EndDate,
                    FormattedStartDate = booking.TourInstance.StartDate.ToString("dd/MM/yyyy"),
                    FormattedEndDate = booking.TourInstance.EndDate.ToString("dd/MM/yyyy"),
                    DurationDays = (booking.TourInstance.EndDate - booking.TourInstance.StartDate).Days + 1,
                    GuideName = booking.TourInstance.Guide?.FullName,
                    
                    LocationName = booking.TourInstance.Tour.Location?.LocationName,
                    City = booking.TourInstance.Tour.Location?.City,
                    Country = booking.TourInstance.Tour.Location?.Country?.CountryName,
                    
                    Seats = booking.Seats,
                    BasePrice = booking.TourInstance.PriceBase,
                    BasePriceTotal = booking.TourInstance.PriceBase * booking.Seats,
                    ServicesTotal = servicesTotal,
                    TotalAmount = booking.TotalAmount,
                    Currency = booking.Currency,
                    FormattedTotalAmount = $"{booking.TotalAmount.ToString("N0")} {booking.Currency}",
                    SpecialRequests = booking.SpecialRequests,
                    
                    BookedServices = bookedServices,
                    ServicesCount = bookedServices.Count
                });
            }

            // Get all reviews for this customer
            var reviews = await _context.Reviews
                .Include(r => r.Tour)
                    .ThenInclude(t => t.TourImages)
                .Include(r => r.Tour)
                    .ThenInclude(t => t.Location)
                        .ThenInclude(l => l!.Country)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reviewViewModels = reviews.Select(r => new CustomerReviewViewModel
            {
                ReviewID = r.ReviewID,
                TourID = r.TourID,
                TourName = r.Tour.TourName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                FormattedCreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                TourImageUrl = r.Tour.TourImages?.FirstOrDefault(i => i.IsPrimary)?.Url ?? 
                               r.Tour.TourImages?.FirstOrDefault()?.Url,
                LocationName = r.Tour.Location?.LocationName,
                Country = r.Tour.Location?.Country?.CountryName
            }).ToList();

            // Calculate statistics
            var totalBookings = bookings.Count;
            var totalSpent = bookings.Where(b => b.Status != "Cancelled" && b.Status != "Rejected").Sum(b => b.TotalAmount);
            var totalReviews = reviews.Count;
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var initials = string.IsNullOrEmpty(customer.FullName) 
                ? customer.Username.Substring(0, Math.Min(2, customer.Username.Length)).ToUpper()
                : customer.FullName.Substring(0, 1).ToUpper();

            return new CustomerDetailsViewModel
            {
                UserID = customer.UserID,
                Username = customer.Username,
                FullName = customer.FullName ?? customer.Username,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                CreatedAt = customer.CreatedAt,
                FormattedCreatedAt = customer.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                
                TotalBookings = totalBookings,
                TotalSpent = totalSpent,
                FormattedTotalSpent = $"{totalSpent.ToString("N0")} VND",
                TotalReviews = totalReviews,
                AverageRating = averageRating,
                IsBanned = customer.IsDeleted,
                
                Bookings = bookingViewModels,
                Reviews = reviewViewModels,
                
                CustomerInitials = initials
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer details for user {UserId}", userId);
            return null;
        }
    }

    public async Task<int> GetCustomersCountAsync()
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .CountAsync(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Customer"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers count");
            return 0;
        }
    }

    private string GetStatusBadgeClass(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "bg-warning",
            "confirmed" => "bg-success",
            "cancelled" => "bg-danger",
            "rejected" => "bg-secondary",
            "completed" => "bg-info",
            _ => "bg-secondary"
        };
    }

    private string GetStatusIcon(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "bi-clock",
            "confirmed" => "bi-check-circle",
            "cancelled" => "bi-x-circle",
            "rejected" => "bi-x-circle",
            "completed" => "bi-star-fill",
            _ => "bi-circle"
        };
    }

    public async Task<(bool Success, string Message)> BanCustomerAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return (false, "Không tìm thấy khách hàng.");
            }

            // Don't allow banning AdministrativeStaff
            if (user.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff"))
            {
                return (false, "Không thể cấm tài khoản quản trị viên.");
            }

            if (user.IsDeleted)
            {
                return (false, "Khách hàng đã bị cấm trước đó.");
            }

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer {UserId} ({Username}) has been banned", userId, user.Username);

            return (true, $"Đã cấm tài khoản {user.FullName ?? user.Username} thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error banning customer {UserId}", userId);
            return (false, "Có lỗi xảy ra khi cấm tài khoản.");
        }
    }

    public async Task<(bool Success, string Message)> UnbanCustomerAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return (false, "Không tìm thấy khách hàng.");
            }

            if (!user.IsDeleted)
            {
                return (false, "Khách hàng chưa bị cấm.");
            }

            user.IsDeleted = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer {UserId} ({Username}) has been unbanned", userId, user.Username);

            return (true, $"Đã bỏ cấm tài khoản {user.FullName ?? user.Username} thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbanning customer {UserId}", userId);
            return (false, "Có lỗi xảy ra khi bỏ cấm tài khoản.");
        }
    }
}
