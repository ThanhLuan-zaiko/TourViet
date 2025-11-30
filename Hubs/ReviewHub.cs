using Microsoft.AspNetCore.SignalR;
using TourViet.DTOs;
using TourViet.Services.Interfaces;

namespace TourViet.Hubs;

public class ReviewHub : Hub
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewHub> _logger;

    public ReviewHub(IReviewService reviewService, ILogger<ReviewHub> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Join a tour-specific group to receive real-time review updates
    /// </summary>
    public async Task JoinTourGroup(string tourId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tour_{tourId}");
        _logger.LogInformation("User {ConnectionId} joined tour group: {TourId}", Context.ConnectionId, tourId);
    }

    /// <summary>
    /// Leave a tour-specific group
    /// </summary>
    public async Task LeaveTourGroup(string tourId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tour_{tourId}");
        _logger.LogInformation("User {ConnectionId} left tour group: {TourId}", Context.ConnectionId, tourId);
    }

    /// <summary>
    /// Submit a new review via WebSocket
    /// </summary>
    public async Task SendReview(CreateReviewRequest request)
    {
        try
        {
            // Get user ID from context (session/claims)
            var userIdClaim = Context.GetHttpContext()?.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                await Clients.Caller.SendAsync("ReviewError", "You must be logged in to submit a review");
                return;
            }

            // Create review via service
            var result = await _reviewService.CreateReviewAsync(request.TourID, userId, request.Rating, request.Comment);

            if (!result.Success || result.Data == null)
            {
                await Clients.Caller.SendAsync("ReviewError", result.Message ?? "Failed to create review");
                return;
            }

            // Convert to DTO
            // Convert to DTO
            var review = (TourViet.Models.Review)result.Data;
            var reviewDto = new ReviewDto
            {
                ReviewID = review.ReviewID,
                TourID = review.TourID,
                UserID = review.UserID,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                UserFullName = review.User?.FullName ?? "Guest",
                UserInitial = review.User?.FullName?.Substring(0, 1).ToUpper() ?? "G"
            };

            // Broadcast to all clients in the tour group
            await Clients.Group($"tour_{request.TourID}")
                .SendAsync("ReceiveReview", reviewDto);

            _logger.LogInformation("Review {ReviewId} broadcast to tour group {TourId}", 
                review.ReviewID, request.TourID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendReview");
            await Clients.Caller.SendAsync("ReviewError", "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Update an existing review via WebSocket
    /// </summary>
    public async Task UpdateReview(UpdateReviewRequest request)
    {
        try
        {
            var userIdClaim = Context.GetHttpContext()?.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                await Clients.Caller.SendAsync("ReviewError", "You must be logged in to update a review");
                return;
            }

            var result = await _reviewService.UpdateReviewAsync(request.ReviewID, userId, request.Rating, request.Comment);

            if (!result.Success || result.Data == null)
            {
                await Clients.Caller.SendAsync("ReviewError", result.Message ?? "Failed to update review");
                return;
            }

            var review = (TourViet.Models.Review)result.Data;
            var reviewDto = new ReviewDto
            {
                ReviewID = review.ReviewID,
                TourID = review.TourID,
                UserID = review.UserID,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                UserFullName = review.User?.FullName ?? "Guest",
                UserInitial = review.User?.FullName?.Substring(0, 1).ToUpper() ?? "G"
            };

            // Broadcast update to all clients in the tour group
            await Clients.Group($"tour_{review.TourID}")
                .SendAsync("ReviewUpdated", reviewDto);

            _logger.LogInformation("Review {ReviewId} update broadcast to tour group {TourId}", 
                review.ReviewID, review.TourID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateReview");
            await Clients.Caller.SendAsync("ReviewError", "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Delete a review via WebSocket
    /// </summary>
    public async Task DeleteReview(Guid reviewId, Guid tourId)
    {
        try
        {
            var userIdClaim = Context.GetHttpContext()?.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                await Clients.Caller.SendAsync("ReviewError", "You must be logged in to delete a review");
                return;
            }

            var result = await _reviewService.DeleteReviewAsync(reviewId, userId);

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ReviewError", result.Message ?? "Failed to delete review");
                return;
            }

            // Broadcast deletion to all clients in the tour group
            await Clients.Group($"tour_{tourId}")
                .SendAsync("ReviewDeleted", reviewId);

            _logger.LogInformation("Review {ReviewId} deletion broadcast to tour group {TourId}", 
                reviewId, tourId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteReview");
            await Clients.Caller.SendAsync("ReviewError", "An unexpected error occurred");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
