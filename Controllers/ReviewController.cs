using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TourViet.DTOs;
using TourViet.Hubs;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Controllers;

[Route("[controller]")]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IHubContext<ReviewHub> _hubContext;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IReviewService reviewService, IHubContext<ReviewHub> hubContext, ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new review (POST /Review/Create)
    /// </summary>
    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        try
        {
            // Get user ID from session
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "You must be logged in to submit a review" });
            }

            var result = await _reviewService.CreateReviewAsync(request.TourID, userId, request.Rating, request.Comment);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            var review = (Review)result.Data;
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

            // Broadcast to real-time clients
            await _hubContext.Clients.Group($"tour_{request.TourID}").SendAsync("ReceiveReview", reviewDto);

            return Ok(new { success = true, data = reviewDto, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the review" });
        }
    }

    /// <summary>
    /// Get paginated reviews for a tour (GET /Review/GetTourReviews/{tourId})
    /// </summary>
    [HttpGet("GetTourReviews/{tourId}")]
    public async Task<IActionResult> GetTourReviews(Guid tourId, [FromQuery] int skip = 0, [FromQuery] int take = 20, [FromQuery] string sortBy = "newest")
    {
        try
        {
            var result = await _reviewService.GetTourReviewsAsync(tourId, skip, take, sortBy);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            var reviews = result.Data as IEnumerable<Review> ?? Enumerable.Empty<Review>();
            var reviewDtos = reviews.Select(r => new ReviewDto
            {
                ReviewID = r.ReviewID,
                TourID = r.TourID,
                UserID = r.UserID,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UserFullName = r.User?.FullName ?? "Guest",
                UserInitial = r.User?.FullName?.Substring(0, 1).ToUpper() ?? "G"
            }).ToList() ?? new List<ReviewDto>();

            return Ok(new { success = true, data = reviewDtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews for tour {TourId}", tourId);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching reviews" });
        }
    }

    /// <summary>
    /// Check if current user can review a tour (GET /Review/CanReview/{tourId})
    /// </summary>
    [HttpGet("CanReview/{tourId}")]
    public async Task<IActionResult> CanReview(Guid tourId)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Ok(new { success = true, canReview = false, message = "You must be logged in" });
            }

            var result = await _reviewService.CanUserReviewTourAsync(tourId, userId);
            
            // Explicitly cast result.Data to bool
            bool canReview = result.Data != null && (bool)result.Data;

            return Ok(new { success = true, canReview = canReview, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking review eligibility for tour {TourId}", tourId);
            return StatusCode(500, new { success = false, canReview = false, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get current user's review for a tour (GET /Review/MyReview/{tourId})
    /// </summary>
    [HttpGet("MyReview/{tourId}")]
    public async Task<IActionResult> MyReview(Guid tourId)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "You must be logged in" });
            }

            var result = await _reviewService.GetUserReviewForTourAsync(tourId, userId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            if (result.Data == null)
            {
                return Ok(new { success = true, data = (ReviewDto?)null });
            }

            var review = (Review)result.Data;
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

            return Ok(new { success = true, data = reviewDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user review");
            return StatusCode(500, new { success = false, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update an existing review (PUT /Review/Update/{reviewId})
    /// </summary>
    [HttpPut("Update/{reviewId}")]
    public async Task<IActionResult> Update(Guid reviewId, [FromBody] UpdateReviewRequest request)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "You must be logged in" });
            }

            // Ensure ReviewID from URL matches request or is set
            request.ReviewID = reviewId;

            var result = await _reviewService.UpdateReviewAsync(request.ReviewID, userId, request.Rating, request.Comment);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            var review = (Review)result.Data;
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

            // Broadcast update
            await _hubContext.Clients.Group($"tour_{review.TourID}").SendAsync("ReviewUpdated", reviewDto);

            return Ok(new { success = true, data = reviewDto, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review");
            return StatusCode(500, new { success = false, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete a review (DELETE /Review/Delete/{reviewId})
    /// </summary>
    [HttpDelete("Delete/{reviewId}")]
    public async Task<IActionResult> Delete(Guid reviewId)
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { success = false, message = "You must be logged in" });
            }
            
            var result = await _reviewService.DeleteReviewAsync(reviewId, userId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            if (HttpContext.Request.Query.TryGetValue("tourId", out var tourIdStr) && Guid.TryParse(tourIdStr, out var tourId))
            {
                 await _hubContext.Clients.Group($"tour_{tourId}").SendAsync("ReviewDeleted", reviewId);
            }

            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review");
            return StatusCode(500, new { success = false, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get review statistics for a tour (GET /Review/Stats/{tourId})
    /// </summary>
    [HttpGet("Stats/{tourId}")]
    public async Task<IActionResult> Stats(Guid tourId)
    {
        try
        {
            var countResult = await _reviewService.GetTourReviewCountAsync(tourId);
            var avgResult = await _reviewService.GetTourAverageRatingAsync(tourId);

            var stats = new ReviewStatsDto
            {
                TotalReviews = (int)(countResult.Data ?? 0),
                AverageRating = (double)(avgResult.Data ?? 0.0)
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching review stats for tour {TourId}", tourId);
            return StatusCode(500, new { success = false, message = "An error occurred" });
        }
    }
}
