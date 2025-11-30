using TourViet.Models;

namespace TourViet.Services.Interfaces;

public interface IReviewService
{
    /// <summary>
    /// Create a new review for a tour
    /// </summary>
    Task<ServiceResult> CreateReviewAsync(Guid tourId, Guid userId, byte rating, string? comment);

    /// <summary>
    /// Get paginated reviews for a specific tour
    /// </summary>
    Task<ServiceResult> GetTourReviewsAsync(Guid tourId, int skip = 0, int take = 20, string sortBy = "newest");

    /// <summary>
    /// Get the current user's review for a specific tour (if exists)
    /// </summary>
    Task<ServiceResult> GetUserReviewForTourAsync(Guid tourId, Guid userId);

    /// <summary>
    /// Check if a user can review a tour (must have completed booking)
    /// </summary>
    Task<ServiceResult> CanUserReviewTourAsync(Guid tourId, Guid userId);

    /// <summary>
    /// Update an existing review
    /// </summary>
    Task<ServiceResult> UpdateReviewAsync(Guid reviewId, Guid userId, byte rating, string? comment);

    /// <summary>
    /// Delete a review (user can only delete their own)
    /// </summary>
    Task<ServiceResult> DeleteReviewAsync(Guid reviewId, Guid userId);

    /// <summary>
    /// Get total count of reviews for a tour
    /// </summary>
    Task<ServiceResult> GetTourReviewCountAsync(Guid tourId);

    /// <summary>
    /// Get average rating for a tour
    /// </summary>
    Task<ServiceResult> GetTourAverageRatingAsync(Guid tourId);
}
