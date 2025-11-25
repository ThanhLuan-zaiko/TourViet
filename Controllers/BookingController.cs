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
    public IActionResult CheckAuth()
    {
        var userId = HttpContext.Session.GetString("UserId");
        var isAuthenticated = !string.IsNullOrEmpty(userId);

        return Ok(new
        {
            isAuthenticated,
            userId = isAuthenticated ? userId : null,
            userName = isAuthenticated ? HttpContext.Session.GetString("FullName") : null
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
                selectedServices);

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
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
