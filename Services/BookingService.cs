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
    private readonly IPromotionService _promotionService;

    public BookingService(
        TourBookingDbContext context, 
        ILogger<BookingService> logger,
        IPromotionService promotionService)
    {
        _context = context;
        _logger = logger;
        _promotionService = promotionService;
    }

    public async Task<(bool Success, string Message, Guid? BookingId, string? BookingRef)> CreateBookingAsync(
        CreateBookingDto dto, 
        Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 0. Check for existing pending booking
            var tourInstanceForCheck = await _context.TourInstances
                .AsNoTracking()
                .FirstOrDefaultAsync(ti => ti.InstanceID == dto.TourInstanceID);
                
            if (tourInstanceForCheck != null)
            {
                var pendingBookingId = await GetPendingBookingIdAsync(userId, tourInstanceForCheck.TourID);
                if (pendingBookingId.HasValue)
                {
                    return (false, "Bạn đang có đơn đặt tour này ở trạng thái chờ xử lý. Vui lòng đợi xác nhận trước khi đặt tiếp.", null, null);
                }
            }

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

            // NO LIMIT on pending bookings - allow unlimited overbooking
            // But limit each individual booking to not exceed tour capacity
            if (dto.Seats > tourInstance.Capacity)
            {
                return (false, $"Không thể đặt quá {tourInstance.Capacity} chỗ cho tour này.", null, null);
            }
            
            // Soft limit warning for customers
            var totalPendingSeats = await _context.Bookings
                .Where(b => b.InstanceID == dto.TourInstanceID && b.Status == "Pending")
                .SumAsync(b => (int?)b.Seats) ?? 0;
                
            var actualAvailable = tourInstance.Capacity - tourInstance.SeatsBooked;
            
            // Soft limit warning: if more than 50 pending bookings or pending > 5x capacity
            var pendingBookingCount = await _context.Bookings
                .CountAsync(b => b.InstanceID == dto.TourInstanceID && b.Status == "Pending");
                
            string? warningMessage = null;
            if (pendingBookingCount >= 50)
            {
                warningMessage = $"⚠️ Lưu ý: Tour này đang rất hot với nhiều đơn chờ duyệt. Khả năng được xác nhận phụ thuộc vào quyết định của Admin.";
            }
            else if (totalPendingSeats > tourInstance.Capacity * 5)
            {
                warningMessage = $"⚠️ Lưu ý: Tour này đang rất hot. Chỉ còn {actualAvailable} chỗ thực tế.";
            }

            // 2. Calculate total price (including promotions)
            var priceCalculation = await CalculatePriceAsync(
                dto.TourInstanceID, 
                dto.Seats, 
                dto.SelectedServices, 
                dto.CouponCode); // Pass coupon code

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
                TotalAmount = priceCalculation.GrandTotal, // This is now the discounted price
                Currency = priceCalculation.Currency,
                SpecialRequests = dto.SpecialRequests,
                Status = "Pending", // Default status, admin can confirm later
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // 3.1 Record Promotion Redemption if applied
            if (priceCalculation.DiscountAmount > 0 && priceCalculation.AppliedPromotionId.HasValue)
            {
                await _promotionService.RecordRedemptionAsync(
                    booking.BookingID,
                    userId,
                    priceCalculation.AppliedPromotionId.Value,
                    priceCalculation.DiscountAmount,
                    priceCalculation.AppliedCouponId
                );
            }

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
            
            // Status management: Only set SoldOut when CONFIRMED bookings fill capacity
            // Keep status as Open if only pending bookings exist
            if (tourInstance.SeatsBooked >= tourInstance.Capacity)
            {
                tourInstance.Status = "SoldOut";
            }
            else if (tourInstance.Status == "SoldOut" && tourInstance.SeatsBooked < tourInstance.Capacity)
            {
                // Re-open if we have capacity and status was SoldOut
                tourInstance.Status = "Open";
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
        
        // Special handling for Pending -> Confirmed
        if (oldStatus == "Pending" && newStatus == "Confirmed")
        {
            // Check if we have actual capacity available
            var actualAvailable = booking.TourInstance.Capacity - booking.TourInstance.SeatsBooked;
            
            if (actualAvailable < booking.Seats)
            {
                return (false, $"Không thể xác nhận. Chỉ còn {actualAvailable} chỗ thực tế. Booking này yêu cầu {booking.Seats} chỗ.");
            }
        }
        
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

            // Update status based on actual confirmed capacity only
            if (booking.TourInstance.SeatsBooked >= booking.TourInstance.Capacity)
            {
                booking.TourInstance.Status = "SoldOut";
            }
            else if (booking.TourInstance.Status == "SoldOut" && 
                     booking.TourInstance.SeatsBooked < booking.TourInstance.Capacity)
            {
                booking.TourInstance.Status = "Open";
            }

            booking.TourInstance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        // Sync promotion redemption status with booking status
        if (oldStatus == "Pending" && newStatus == "Confirmed")
        {
            // Confirm the promotion redemption when booking is approved
            await _promotionService.ConfirmRedemptionAsync(bookingId);
        }
        else if ((oldStatus == "Pending" || oldStatus == "Confirmed") && newStatus == "Cancelled")
        {
            // Void the promotion redemption to free up usage when booking is cancelled
            await _promotionService.VoidRedemptionAsync(bookingId);
        }
        
        // After confirming, auto-reject excess pending bookings if needed
        if (oldStatus == "Pending" && newStatus == "Confirmed")
        {
            await AutoRejectOverbookedPendingBookingsAsync(booking.TourInstance.InstanceID);
        }

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

private async Task AutoRejectOverbookedPendingBookingsAsync(Guid instanceId)
{
    try
    {
        // Get instance info without loading all bookings
        var instance = await _context.TourInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(ti => ti.InstanceID == instanceId);
            
        if (instance == null) return;
        
        var remainingCapacity = instance.Capacity - instance.SeatsBooked;
        
        if (remainingCapacity <= 0)
        {
            // No capacity left - reject ALL pending bookings with batch update
            var rejectedCount = await _context.Bookings
                .Where(b => b.InstanceID == instanceId && b.Status == "Pending")
                .ExecuteUpdateAsync(b => b
                    .SetProperty(x => x.Status, "Rejected")
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            
            // Reset SeatsHeld to 0 since all pending rejected
            if (rejectedCount > 0)
            {
                var instanceToUpdate = await _context.TourInstances
                    .FirstOrDefaultAsync(ti => ti.InstanceID == instanceId);
                if (instanceToUpdate != null)
                {
                    instanceToUpdate.SeatsHeld = 0;
                    instanceToUpdate.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                
                _logger.LogInformation(
                    "Auto-rejected {Count} pending bookings for instance {InstanceId} (no capacity)",
                    rejectedCount, instanceId);
            }
        }
        else
        {
            // Some capacity remaining - selective rejection
            // Get IDs of bookings to keep (earliest ones that fit)
            var pendingBookings = await _context.Bookings
                .Where(b => b.InstanceID == instanceId && b.Status == "Pending")
                .OrderBy(b => b.CreatedAt)
                .Select(b => new { b.BookingID, b.Seats })
                .ToListAsync();
            
            var bookingsToKeep = new List<Guid>();
            var capacityLeft = remainingCapacity;
            var totalSeatsToReject = 0;
            
            foreach (var booking in pendingBookings)
            {
                if (capacityLeft >= booking.Seats)
                {
                    bookingsToKeep.Add(booking.BookingID);
                    capacityLeft -= booking.Seats;
                }
                else
                {
                    totalSeatsToReject += booking.Seats;
                }
            }
            
            // Batch reject bookings NOT in the keep list
            if (bookingsToKeep.Count < pendingBookings.Count)
            {
                var rejectedCount = await _context.Bookings
                    .Where(b => b.InstanceID == instanceId && 
                                b.Status == "Pending" && 
                                !bookingsToKeep.Contains(b.BookingID))
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(x => x.Status, "Rejected")
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
                
                // Update SeatsHeld
                if (rejectedCount > 0)
                {
                    var instanceToUpdate = await _context.TourInstances
                        .FirstOrDefaultAsync(ti => ti.InstanceID == instanceId);
                    if (instanceToUpdate != null)
                    {
                        instanceToUpdate.SeatsHeld = Math.Max(0, instanceToUpdate.SeatsHeld - totalSeatsToReject);
                        instanceToUpdate.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    
                    _logger.LogInformation(
                        "Auto-rejected {Count} pending bookings for instance {InstanceId} ({Kept} kept, {Capacity} capacity left)",
                        rejectedCount, instanceId, bookingsToKeep.Count, capacityLeft);
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error auto-rejecting overbooked pending bookings for instance {InstanceId}", instanceId);
    }
}

    public async Task<PriceCalculationDto?> CalculatePriceAsync(
        Guid instanceId, 
        int seats, 
        List<SelectedServiceDto> selectedServices,
        string? couponCode = null)
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

            var subTotalBeforeDiscount = basePriceTotal + servicesTotal;
            
            // Calculate Discount
            // We need userId for per-user limits, but CalculatePrice might be called by anonymous users (or before login)
            // For now, we'll pass Guid.Empty if we don't have a user context here, but ideally we should get it from context if available.
            // However, CalculatePriceAsync in interface doesn't have userId.
            // Let's assume for price check we use a dummy user or modify interface later.
            // For now, let's use Guid.Empty and handle it in PromotionService (it might skip user limit checks).
            
            var discountResult = await _promotionService.CalculateDiscountAsync(
                Guid.Empty, // No user context in this method yet
                tourInstance.TourID,
                instanceId,
                subTotalBeforeDiscount,
                seats,
                couponCode
            );

            return new PriceCalculationDto
            {
                BasePrice = basePrice,
                Seats = seats,
                BasePriceTotal = basePriceTotal,
                Services = services,
                ServicesTotal = servicesTotal,
                GrandTotal = subTotalBeforeDiscount - discountResult.DiscountAmount,
                Currency = tourInstance.Currency,
                
                // New fields for promotion
                DiscountAmount = discountResult.DiscountAmount,
                AppliedPromotionId = discountResult.PromotionID,
                AppliedCouponId = discountResult.CouponID,
                PromotionMessage = discountResult.Message
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

    public async Task<Guid?> GetPendingBookingIdAsync(Guid userId, Guid tourId)
    {
        var booking = await _context.Bookings
            .Include(b => b.TourInstance)
            .FirstOrDefaultAsync(b => b.UserID == userId && 
                                    b.TourInstance.TourID == tourId && 
                                    b.Status == "Pending");
        
        return booking?.BookingID;
    }

    public async Task<ServiceResult> ProcessPaymentAsync(Guid bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null)
        {
            return new ServiceResult { Success = false, Message = "Không tìm thấy booking." };
        }

        if (booking.Status != "Confirmed")
        {
            return new ServiceResult { Success = false, Message = "Chỉ có thể thanh toán cho đơn đặt tour đã được xác nhận." };
        }

        // Check if already paid
        var isPaid = await _context.Payments.AnyAsync(p => p.BookingID == bookingId && p.Status == "Completed");
        if (isPaid)
        {
            return new ServiceResult { Success = false, Message = "Đơn đặt tour này đã được thanh toán." };
        }

        // Create payment record
        var payment = new Payment
        {
            BookingID = bookingId,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = "CreditCard", // Simulated
            Status = "Completed",
            TransactionRef = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            PaidAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        
        // Update booking status to Completed
        booking.Status = "Completed";
        booking.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return new ServiceResult { Success = true, Message = "Thanh toán thành công." };
    }

    public async Task<bool> IsBookingPaidAsync(Guid bookingId)
    {
        return await _context.Payments.AnyAsync(p => p.BookingID == bookingId && p.Status == "Completed");
    }
}
