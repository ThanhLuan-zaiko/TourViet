using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.DTOs;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Services;

public class BookingService : IBookingService
{
    private readonly TourBookingDbContext _context;
    private readonly ILogger<BookingService> _logger;

    public BookingService(TourBookingDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Guid? BookingId, string? BookingRef)> CreateBookingAsync(
        CreateBookingDto dto, 
        Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate tour instance exists and has available seats
            var tourInstance = await _context.TourInstances
                .Include(ti => ti.Tour)
                .FirstOrDefaultAsync(ti => ti.InstanceID == dto.TourInstanceID);

            if (tourInstance == null)
            {
                return (false, "Không tìm thấy chuyến đi này.", null, null);
            }

            if (tourInstance.Status != "Open")
            {
                return (false, "Chuyến đi này hiện không nhận đặt chỗ.", null, null);
            }

            var seatsAvailable = tourInstance.Capacity - tourInstance.SeatsBooked - tourInstance.SeatsHeld;
            if (seatsAvailable < dto.Seats)
            {
                return (false, $"Không đủ chỗ trống. Chỉ còn {seatsAvailable} chỗ.", null, null);
            }

            // 2. Calculate total price
            var priceCalculation = await CalculatePriceAsync(dto.TourInstanceID, dto.Seats, dto.SelectedServices);
            if (priceCalculation == null)
            {
                return (false, "Không thể tính giá tour. Vui lòng thử lại.", null, null);
            }

            // 3. Create booking
            var booking = new Booking
            {
                BookingID = Guid.NewGuid(),
                InstanceID = dto.TourInstanceID,
                UserID = userId,
                BookingRef = GenerateBookingReference(),
                Seats = dto.Seats,
                TotalAmount = priceCalculation.GrandTotal,
                Currency = priceCalculation.Currency,
                SpecialRequests = dto.SpecialRequests,
                Status = "Pending", // Default status, admin can confirm later
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // 4. Add selected services to BookingServices
            foreach (var selectedService in dto.SelectedServices)
            {
                var service = await _context.Services.FindAsync(selectedService.ServiceID);
                if (service == null) continue;

                // Find the service in tour services to get the correct price
                var tourService = await _context.TourServices
                    .FirstOrDefaultAsync(ts => 
                        ts.TourID == tourInstance.TourID && 
                        ts.ServiceID == selectedService.ServiceID);

                decimal servicePrice = tourService?.PriceOverride ?? service.Price;

                var bookingService = new Models.BookingService
                {
                    BookingServiceID = Guid.NewGuid(),
                    BookingID = booking.BookingID,
                    ServiceID = selectedService.ServiceID,
                    Quantity = selectedService.Quantity,
                    PriceAtBooking = servicePrice,
                    Currency = tourService?.Currency ?? service.Currency,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BookingServices.Add(bookingService);
            }

            // 5. Update tour instance seats (hold seats until confirmed)
            tourInstance.SeatsHeld += dto.Seats;
            
            // Check if full
            if (tourInstance.SeatsBooked + tourInstance.SeatsHeld >= tourInstance.Capacity)
            {
                tourInstance.Status = "SoldOut";
            }
            
            tourInstance.UpdatedAt = DateTime.UtcNow;

            // 6. Save all changes
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Booking created successfully: {BookingRef} for user {UserId}, instance {InstanceId}",
                booking.BookingRef, userId, dto.TourInstanceID);

            return (true, "Đặt tour thành công!", booking.BookingID, booking.BookingRef);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating booking for user {UserId}, instance {InstanceId}", 
                userId, dto.TourInstanceID);
            return (false, "Đã xảy ra lỗi khi đặt tour. Vui lòng thử lại sau.", null, null);
        }
    }

    public async Task<BookingDetailsDto?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.TourInstance)
                .ThenInclude(ti => ti.Tour)
                    .ThenInclude(t => t.Category)
            .Include(b => b.TourInstance)
                .ThenInclude(ti => ti.Tour)
                    .ThenInclude(t => t.Location)
                        .ThenInclude(l => l!.Country)
            .Include(b => b.TourInstance)
                .ThenInclude(ti => ti.Guide)
            .FirstOrDefaultAsync(b => b.BookingID == bookingId);

        if (booking == null) return null;

        // Get booked services
        var bookedServices = await _context.BookingServices
            .Include(bs => bs.Service)
            .Where(bs => bs.BookingID == bookingId)
            .Select(bs => new BookedServiceDto
            {
                ServiceID = bs.ServiceID,
                ServiceName = bs.Service.ServiceName,
                Quantity = bs.Quantity,
                PriceAtBooking = bs.PriceAtBooking,
                Currency = bs.Currency,
                SubTotal = bs.PriceAtBooking * bs.Quantity
            })
            .ToListAsync();

        var servicesTotal = bookedServices.Sum(s => s.SubTotal);

            return new BookingDetailsDto
            {
                BookingID = booking.BookingID,
                BookingRef = booking.BookingRef,
                Status = booking.Status,
                BookingDate = booking.CreatedAt,
                Seats = booking.Seats,
                TotalAmount = booking.TotalAmount,
                Currency = booking.Currency,
                SpecialRequests = booking.SpecialRequests,

                // Customer Info
            UserID = booking.UserID,
            CustomerName = booking.User?.FullName ?? "N/A",
            CustomerEmail = booking.User?.Email ?? "",
            CustomerPhone = booking.User?.Phone,
            CustomerAddress = booking.User?.Address,

            // Tour Info
            TourID = booking.TourInstance.TourID,
            TourName = booking.TourInstance.Tour.TourName,
            TourDescription = booking.TourInstance.Tour.Description,
            TourCategory = booking.TourInstance.Tour.Category?.CategoryName,
            TourImageUrl = booking.TourInstance.Tour.TourImages?.FirstOrDefault(i => i.IsPrimary)?.Url ?? booking.TourInstance.Tour.TourImages?.FirstOrDefault()?.Url,

            // Instance Info
            InstanceID = booking.InstanceID,
            StartDate = booking.TourInstance.StartDate,
            EndDate = booking.TourInstance.EndDate,
            DurationDays = (booking.TourInstance.EndDate - booking.TourInstance.StartDate).Days + 1,
            PriceBase = booking.TourInstance.PriceBase,
            GuideName = booking.TourInstance.Guide?.FullName,

            // Location Info
            LocationName = booking.TourInstance.Tour.Location?.LocationName,
            City = booking.TourInstance.Tour.Location?.City,
            Country = booking.TourInstance.Tour.Location?.Country?.CountryName,

            // Services
            BookedServices = bookedServices,
            ServicesTotal = servicesTotal
        };
    }

    public async Task<List<BookingDetailsDto>> GetAllBookingsAsync()
    {
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
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var result = new List<BookingDetailsDto>();

        foreach (var booking in bookings)
        {
            var bookedServices = await _context.BookingServices
                .Include(bs => bs.Service)
                .Where(bs => bs.BookingID == booking.BookingID)
                .Select(bs => new BookedServiceDto
                {
                    ServiceID = bs.ServiceID,
                    ServiceName = bs.Service.ServiceName,
                    Quantity = bs.Quantity,
                    PriceAtBooking = bs.PriceAtBooking,
                    Currency = bs.Currency,
                    SubTotal = bs.PriceAtBooking * bs.Quantity
                })
                .ToListAsync();

            var servicesTotal = bookedServices.Sum(s => s.SubTotal);

            result.Add(new BookingDetailsDto
            {
                BookingID = booking.BookingID,
                BookingRef = booking.BookingRef,
                Status = booking.Status,
                BookingDate = booking.CreatedAt,
                Seats = booking.Seats,
                TotalAmount = booking.TotalAmount,
                Currency = booking.Currency,
                SpecialRequests = booking.SpecialRequests,

                // Customer Info
                UserID = booking.UserID,
                CustomerName = booking.User?.FullName ?? "N/A",
                CustomerEmail = booking.User?.Email ?? "",
                CustomerPhone = booking.User?.Phone,
                CustomerAddress = booking.User?.Address,

                // Tour Info
                TourID = booking.TourInstance.TourID,
                TourName = booking.TourInstance.Tour.TourName,
                TourDescription = booking.TourInstance.Tour.Description,
                TourCategory = booking.TourInstance.Tour.Category?.CategoryName,
                TourImageUrl = booking.TourInstance.Tour.TourImages?.FirstOrDefault(i => i.IsPrimary)?.Url ?? booking.TourInstance.Tour.TourImages?.FirstOrDefault()?.Url,
                TourImages = booking.TourInstance.Tour.TourImages?.Select(i => i.Url).ToList() ?? new List<string>(),

                // Instance Info
                InstanceID = booking.InstanceID,
                StartDate = booking.TourInstance.StartDate,
                EndDate = booking.TourInstance.EndDate,
                DurationDays = (booking.TourInstance.EndDate - booking.TourInstance.StartDate).Days + 1,
                PriceBase = booking.TourInstance.PriceBase,
                GuideName = booking.TourInstance.Guide?.FullName,

                // Location Info
                LocationName = booking.TourInstance.Tour.Location?.LocationName,
                City = booking.TourInstance.Tour.Location?.City,
                Country = booking.TourInstance.Tour.Location?.Country?.CountryName,

                // Services
                BookedServices = bookedServices,
                ServicesTotal = servicesTotal
            });
        }

        return result;
    }

    public async Task<List<BookingDetailsDto>> GetUserBookingsAsync(Guid userId)
    {
        var allBookings = await GetAllBookingsAsync();
        return allBookings.Where(b => b.UserID == userId).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateBookingStatusAsync(Guid bookingId, string newStatus)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.TourInstance)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
            {
                return (false, "Không tìm thấy booking.");
            }

            var oldStatus = booking.Status;
            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.UtcNow;

            // Update seat counts based on status change
            if (oldStatus == "Pending" && newStatus == "Confirmed")
            {
                // Move from held to booked
                booking.TourInstance.SeatsHeld = Math.Max(0, booking.TourInstance.SeatsHeld - booking.Seats);
                booking.TourInstance.SeatsBooked += booking.Seats;
            }
            else if ((oldStatus == "Pending" || oldStatus == "Confirmed") && newStatus == "Cancelled")
            {
                // Release seats
                if (oldStatus == "Pending")
                {
                    booking.TourInstance.SeatsHeld = Math.Max(0, booking.TourInstance.SeatsHeld - booking.Seats);
                }
                else if (oldStatus == "Confirmed")
                {
                    booking.TourInstance.SeatsBooked = Math.Max(0, booking.TourInstance.SeatsBooked - booking.Seats);
                }
            }

            // Update status based on availability
            if (booking.TourInstance.SeatsBooked + booking.TourInstance.SeatsHeld >= booking.TourInstance.Capacity)
            {
                booking.TourInstance.Status = "SoldOut";
            }
            else if (booking.TourInstance.Status == "SoldOut" && 
                     booking.TourInstance.SeatsBooked + booking.TourInstance.SeatsHeld < booking.TourInstance.Capacity)
            {
                booking.TourInstance.Status = "Open";
            }

            booking.TourInstance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking status updated: {BookingRef} from {OldStatus} to {NewStatus}",
                booking.BookingRef, oldStatus, newStatus);

            return (true, $"Cập nhật trạng thái thành công: {newStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking status for {BookingId}", bookingId);
            return (false, "Đã xảy ra lỗi khi cập nhật trạng thái.");
        }
    }

    public async Task<PriceCalculationDto?> CalculatePriceAsync(
        Guid instanceId, 
        int seats, 
        List<SelectedServiceDto> selectedServices)
    {
        try
        {
            var tourInstance = await _context.TourInstances
                .Include(ti => ti.Tour)
                .FirstOrDefaultAsync(ti => ti.InstanceID == instanceId);

            if (tourInstance == null) return null;

            var basePrice = tourInstance.PriceBase;
            var basePriceTotal = basePrice * seats;

            var services = new List<ServicePriceDto>();
            decimal servicesTotal = 0;

            foreach (var selectedService in selectedServices)
            {
                var service = await _context.Services.FindAsync(selectedService.ServiceID);
                if (service == null) continue;

                // Check if there's a tour-specific price override
                var tourService = await _context.TourServices
                    .FirstOrDefaultAsync(ts => 
                        ts.TourID == tourInstance.TourID && 
                        ts.ServiceID == selectedService.ServiceID);

                decimal unitPrice = tourService?.PriceOverride ?? service.Price;
                decimal subTotal = unitPrice * selectedService.Quantity * seats; // Price per person per seat

                services.Add(new ServicePriceDto
                {
                    ServiceID = service.ServiceID,
                    ServiceName = service.ServiceName,
                    Quantity = selectedService.Quantity * seats,
                    UnitPrice = unitPrice,
                    SubTotal = subTotal
                });

                servicesTotal += subTotal;
            }

            return new PriceCalculationDto
            {
                BasePrice = basePrice,
                Seats = seats,
                BasePriceTotal = basePriceTotal,
                Services = services,
                ServicesTotal = servicesTotal,
                GrandTotal = basePriceTotal + servicesTotal,
                Currency = tourInstance.Currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price for instance {InstanceId}", instanceId);
            return null;
        }
    }

    public string GenerateBookingReference()
    {
        // Generate format: BK-YYYYMMDD-XXXXX (e.g., BK-20251125-A3F9D)
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
        return $"BK-{datePart}-{randomPart}";
    }
}
