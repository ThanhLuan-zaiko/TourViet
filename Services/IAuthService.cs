using TourViet.DTOs.Requests;
using TourViet.DTOs.Responses;
using TourViet.Models;

namespace TourViet.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<(bool success, string message)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(Guid userId);
    UserInfo MapToUserInfo(User user);
}

