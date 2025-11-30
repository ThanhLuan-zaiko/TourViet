using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Services;

public class ReviewService : IReviewService
{
    private readonly TourBookingDbContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(TourBookingDbContext context, ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult> CreateReviewAsync(Guid tourId, Guid userId, byte rating, string? comment)
    {
        try
        {
            // Validate rating
            if (rating < 1 || rating > 5)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Rating must be between 1 and 5"
                };
            }

            // Check if tour exists
            var tourExists = await _context.Tours.AnyAsync(t => t.TourID == tourId && !t.IsDeleted);
            if (!tourExists)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Tour not found"
                };
            }

            // Check if user can review (has completed booking)
            var canReview = await CanUserReviewTourAsync(tourId, userId);
            if (!canReview.Success || !(bool)(canReview.Data ?? false))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = canReview.Message ?? "You must complete a booking for this tour before reviewing"
                };
            }

            // Check if user already reviewed this tour
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.TourID == tourId && r.UserID == userId);

            if (existingReview != null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "You have already reviewed this tour. Please update your existing review instead."
                };
            }

            // Create new review
            var review = new Review
            {
                ReviewID = Guid.NewGuid(),
                TourID = tourId,
                UserID = userId,
                Rating = rating,
                Comment = comment?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Load navigation properties
            await _context.Entry(review)
                .Reference(r => r.User)
                .LoadAsync();

            _logger.LogInformation("Review created: {ReviewId} by User {UserId} for Tour {TourId}", 
                review.ReviewID, userId, tourId);

            return new ServiceResult
            {
                Success = true,
                Data = review,
                Message = "Review created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for Tour {TourId} by User {UserId}", tourId, userId);
            return new ServiceResult
            {
                Success = false,
                Message = "An error occurred while creating the review"
            };
        }
    }

    public async Task<ServiceResult> GetTourReviewsAsync(Guid tourId, int skip = 0, int take = 20, string sortBy = "newest")
    {
        try
        {
            IQueryable<Review> query = _context.Reviews
                .Where(r => r.TourID == tourId)
                .Include(r => r.User);

            // Apply sorting based on sortBy parameter
            query = sortBy?.ToLower() switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "highest_rating" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                _ => query.OrderByDescending(r => r.CreatedAt) // "newest" or default
            };

            var reviews = await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new ServiceResult
            {
                Success = true,
                Data = reviews
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews for Tour {TourId}", tourId);
            return new ServiceResult
            {
                Success = false,
                Message = "An error occurred while fetching reviews"
            };
        }
    }

    public async Task<ServiceResult> GetUserReviewForTourAsync(Guid tourId, Guid userId)
    {
        try
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.TourID == tourId && r.UserID == userId);

            return new ServiceResult
            {
                Success = true,
                Data = review
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user review for Tour {TourId} and User {UserId}", tourId, userId);
            return new ServiceResult
            {
                Success = false,
                Message = "An error occurred while fetching your review"
            };
        }
    }

    public async Task<ServiceResult> CanUserReviewTourAsync(Guid tourId, Guid userId)
    {
        try
        {
            // Check if user has at least one COMPLETED booking for this tour
            var hasCompletedBooking = await _context.Bookings
                .Include(b => b.TourInstance)
                .AnyAsync(b => 
                    b.UserID == userId && 
                    b.TourInstance.TourID == tourId &&
                    b.Status == "Completed");

            if (!hasCompletedBooking)
            {
                return new ServiceResult
                {
                    Success = false,
                    Data = false,
                    Message = "Bạn cần hoàn thành tour này trước khi có thể đánh giá"
                };
            }

            return new ServiceResult
            {
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can review Tour {TourId}", tourId);
            return new ServiceResult
            {
                Success = false,
                Data = false,
                Message = "An error occurred while checking review eligibility"
            };
        }
    }

    public async Task<ServiceResult> UpdateReviewAsync(Guid reviewId, Guid userId, byte rating, string? comment)
    {
        try
        {
            // Validate rating
            if (rating < 1 || rating > 5)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Rating must be between 1 and 5"
                };
            }

            // Find review
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

            if (review == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            // Verify ownership
            if (review.UserID != userId)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "You can only update your own reviews"
                };
            }

            // Update review
            review.Rating = rating;
            review.Comment = comment?.Trim();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Review updated: {ReviewId} by User {UserId}", reviewId, userId);

            return new ServiceResult
            {
                Success = true,
                Data = review,
                Message = "Review updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
            return new ServiceResult
            {
                Success = false,
                Message = "An error occurred while updating the review"
            };
        }
    }

    public async Task<ServiceResult> DeleteReviewAsync(Guid reviewId, Guid userId)
    {
        try
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

            if (review == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Data = false,
                    Message = "Review not found"
                };
            }

            // Verify ownership
            if (review.UserID != userId)
            {
                return new ServiceResult
                {
                    Success = false,
                    Data = false,
                    Message = "You can only delete your own reviews"
                };
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Review deleted: {ReviewId} by User {UserId}", reviewId, userId);

            return new ServiceResult
            {
                Success = true,
                Data = true,
                Message = "Review deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return new ServiceResult
            {
                Success = false,
                Data = false,
                Message = "An error occurred while deleting the review"
            };
        }
    }

    public async Task<ServiceResult> GetTourReviewCountAsync(Guid tourId)
    {
        try
        {
            var count = await _context.Reviews
                .CountAsync(r => r.TourID == tourId);

            return new ServiceResult
            {
                Success = true,
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting reviews for Tour {TourId}", tourId);
            return new ServiceResult
            {
                Success = false,
                Message = "An error occurred while counting reviews"
            };
        }
    }

    public async Task<ServiceResult> GetTourAverageRatingAsync(Guid tourId)
    {
        try
        {
            var reviews = await _context.Reviews
                .Where(r => r.TourID == tourId)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new ServiceResult
                {
                    Success = true,
                    Data = 0
                };
            }

            var average = reviews.Average(r => r.Rating);

            return new ServiceResult
            {
                Success = true,
                Data = average
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average rating for Tour {TourId}", tourId);
            return new ServiceResult
            {
                Success = false,
                Message = "An error occurred while calculating average rating"
            };
        }
    }
}
