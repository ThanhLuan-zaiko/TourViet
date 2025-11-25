using TourViet.DTOs;

namespace TourViet.Services.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Creates a new booking for a tour instance
    /// </summary>
    Task<(bool Success, string Message, Guid? BookingId, string? BookingRef)> CreateBookingAsync(
        CreateBookingDto dto, 
        Guid userId);

    /// <summary>
    /// Gets detailed booking information by booking ID
    /// </summary>
    Task<BookingDetailsDto?> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Gets all bookings for administrative management
    /// </summary>
    Task<List<BookingDetailsDto>> GetAllBookingsAsync();

    /// <summary>
    /// Gets bookings for a specific user
    /// </summary>
    Task<List<BookingDetailsDto>> GetUserBookingsAsync(Guid userId);

    /// <summary>
    /// Updates the status of a booking
    /// </summary>
    Task<(bool Success, string Message)> UpdateBookingStatusAsync(Guid bookingId, string newStatus);

    /// <summary>
    /// Calculates the total price for a booking without creating it
    /// </summary>
    Task<PriceCalculationDto?> CalculatePriceAsync(
        Guid instanceId, 
        int seats, 
        List<SelectedServiceDto> selectedServices);

    /// <summary>
    /// Generates a unique booking reference
    /// </summary>
    string GenerateBookingReference();
}
