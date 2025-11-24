using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Controllers
{
    /// <summary>
    /// API Controller for managing tour images with real-time upload and delete operations.
    /// </summary>
    public class TourImageController : Controller
    {
        private readonly TourBookingDbContext _context;
        private readonly IImageService _imageService;
        private readonly ILogger<TourImageController> _logger;

        public TourImageController(
            TourBookingDbContext context,
            IImageService imageService,
            ILogger<TourImageController> logger)
        {
            _context = context;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>
        /// Upload multiple images for a tour (real-time AJAX endpoint).
        /// </summary>
        /// <param name="tourId">The ID of the tour</param>
        /// <param name="files">Collection of image files to upload</param>
        /// <returns>JSON result with uploaded image information</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid tourId, IFormFileCollection files)
        {
            try
            {
                // Validate tour exists
                var tour = await _context.Tours.FindAsync(tourId);
                if (tour == null)
                {
                    return Json(new { success = false, message = "Tour không tồn tại." });
                }

                // Check if user has permission (AdministrativeStaff role)
                var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
                if (!userRoles.Contains("AdministrativeStaff"))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
                }

                // Validate files
                if (files == null || files.Count == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một hình ảnh." });
                }

                // Get current max sort order
                var maxSortOrder = await _imageService.GetMaxSortOrderAsync(tourId);

                // Process and save images
                var tourImages = await _imageService.ProcessMultipleImagesAsync(tourId, files, maxSortOrder + 1);

                // Get current user ID for tracking
                var userIdString = HttpContext.Session.GetString("UserID");
                if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
                {
                    foreach (var image in tourImages)
                    {
                        image.UploadedBy = userId;
                    }
                }

                // Add to database
                await _context.TourImages.AddRangeAsync(tourImages);
                await _context.SaveChangesAsync();

                // Prepare response
                var uploadedImages = tourImages.Select(img => new
                {
                    imageId = img.ImageID.ToString(),
                    url = img.Url,
                    fileName = img.FileName,
                    sortOrder = img.SortOrder
                }).ToList();

                _logger.LogInformation("Successfully uploaded {Count} images for tour {TourId}", tourImages.Count(), tourId);

                return Json(new
                {
                    success = true,
                    message = $"Đã upload thành công {tourImages.Count()} hình ảnh.",
                    images = uploadedImages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading images for tour {TourId}", tourId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi upload hình ảnh: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete a tour image (real-time AJAX endpoint).
        /// </summary>
        /// <param name="imageId">The ID of the image to delete</param>
        /// <returns>JSON result indicating success or failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid imageId)
        {
            try
            {
                // Find the image
                var image = await _context.TourImages.FindAsync(imageId);
                if (image == null)
                {
                    return Json(new { success = false, message = "Hình ảnh không tồn tại hoặc đã bị xóa." });
                }

                // Check if user has permission
                var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
                if (!userRoles.Contains("AdministrativeStaff"))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
                }

                // Delete image using service (handles both DB and file system)
                var deleted = await _imageService.DeleteImageAsync(imageId);

                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted image {ImageId} for tour {TourId}", imageId, image.TourID);
                    return Json(new { success = true, message = "Đã xóa hình ảnh thành công." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa hình ảnh." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa hình ảnh: " + ex.Message });
            }
        }
    }
}
