using Microsoft.AspNetCore.Mvc;
using TourViet.DTOs;
using TourViet.Services.Interfaces;

namespace TourViet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    [HttpGet("check-auth")]
    public async Task<IActionResult> CheckAuth(Guid? tourId = null)
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        var isAuthenticated = !string.IsNullOrEmpty(userIdString);
        Guid? pendingBookingId = null;

        if (isAuthenticated && tourId.HasValue && Guid.TryParse(userIdString, out var userId))
        {
            pendingBookingId = await _bookingService.GetPendingBookingIdAsync(userId, tourId.Value);
        }

        return Ok(new
        {
            isAuthenticated,
            userId = isAuthenticated ? userIdString : null,
            userName = isAuthenticated ? HttpContext.Session.GetString("FullName") : null,
            hasPendingBooking = pendingBookingId.HasValue,
            pendingBookingId
        });
    }

    /// <summary>
    /// Calculate price for a booking without creating it
    /// </summary>
    [HttpPost("calculate-price")]
    public async Task<IActionResult> CalculatePrice([FromBody] PriceCalculationRequest request)
    {
        try
        {
            var selectedServices = request.SelectedServiceIds?
                .Select(id => new SelectedServiceDto { ServiceID = id, Quantity = 1 })
                .ToList() ?? new List<SelectedServiceDto>();

            var calculation = await _bookingService.CalculatePriceAsync(
                request.InstanceId,
                request.Seats,
                selectedServices,
                request.CouponCode);

            if (calculation == null)
            {
                return BadRequest(new { success = false, message = "Không thể tính giá. Vui lòng thử lại." });
            }

            return Ok(new
            {
                success = true,
                data = calculation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price");
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tính giá." });
        }
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để đặt tour." });
            }

            if (!Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest(new { success = false, message = "User ID không hợp lệ." });
            }

            // Validate dto
            if (dto.TourInstanceID == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn ngày khởi hành." });
            }

            if (dto.Seats <= 0)
            {
                return BadRequest(new { success = false, message = "Số lượng chỗ phải lớn hơn 0." });
            }

            // DTO now includes CouponCode, which is handled inside CreateBookingAsync
            var result = await _bookingService.CreateBookingAsync(dto, userId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    bookingId = result.BookingId,
                    bookingRef = result.BookingRef
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi đặt tour." });
        }
    }

    /// <summary>
    /// Process payment for a booking (User)
    /// </summary>
    [HttpPost("pay/{id}")]
    public async Task<IActionResult> PayBooking(Guid id)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này." });
            }

            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy booking." });
            }

            if (booking.UserID != userId)
            {
                return Forbid();
            }

            var result = await _bookingService.ProcessPaymentAsync(id);

            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying for booking {BookingId}", id);
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi thanh toán." });
        }
    }

    /// <summary>
    /// Cancel a pending booking (User)
    /// </summary>
    [HttpPost("cancel/{id}")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này." });
            }

            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy booking." });
            }

            if (booking.UserID != userId)
            {
                return Forbid();
            }

            if (booking.Status != "Pending" && booking.Status != "Confirmed")
            {
                return BadRequest(new { success = false, message = "Chỉ có thể hủy đơn đặt tour đang ở trạng thái chờ xử lý hoặc đã xác nhận (chưa thanh toán)." });
            }

            // Check if already paid (cannot cancel if paid - simplistic logic for now)
            var isPaid = await _bookingService.IsBookingPaidAsync(id);
            if (isPaid)
            {
                 return BadRequest(new { success = false, message = "Không thể hủy đơn đặt tour đã thanh toán." });
            }

            var result = await _bookingService.UpdateBookingStatusAsync(id, "Cancelled");

            if (result.Success)
            {
                return Ok(new { success = true, message = "Hủy đặt tour thành công." });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi hủy đặt tour." });
        }
    }

    /// <summary>
    /// Get booking details by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy booking." });
            }

            // Check if user has permission to view this booking
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? Array.Empty<string>();
            var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

            if (!isAdministrativeStaff && booking.UserID.ToString() != userIdString)
            {
                return Forbid();
            }

            return Ok(new
            {
                success = true,
                data = booking
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking {BookingId}", id);
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi." });
        }
    }

    /// <summary>
    /// Update booking status (Admin only)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var result = await _bookingService.UpdateBookingStatusAsync(id, request.Status);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking status for {BookingId}", id);
            return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi." });
        }
    }
}

// Request DTOs
public class PriceCalculationRequest
{
    public Guid InstanceId { get; set; }
    public int Seats { get; set; }
    public List<Guid>? SelectedServiceIds { get; set; }
    public string? CouponCode { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
