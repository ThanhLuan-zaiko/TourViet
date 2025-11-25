using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.DTOs.Requests;
using TourViet.DTOs.Responses;
using TourViet.Models;

namespace TourViet.Services;

public class AuthService : IAuthService
{
    private readonly TourBookingDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<AuthService> _logger;
    private const int MaxLoginAttempts = 5;
    private const int MaxRegisterAttempts = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AccountLockoutDuration = TimeSpan.FromMinutes(30);

    public AuthService(
        TourBookingDbContext context, 
        IPasswordHasher passwordHasher,
        IRateLimitService rateLimitService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Rate limiting check
            var rateLimitKey = $"register:{request.Email}";
            var isAllowed = await _rateLimitService.IsAllowedAsync(rateLimitKey, MaxRegisterAttempts, RateLimitWindow);
            
            if (!isAllowed)
            {
                var attempts = await _rateLimitService.GetAttemptCountAsync(rateLimitKey);
                _logger.LogWarning("Registration rate limit exceeded for email: {Email}, attempts: {Attempts}", request.Email, attempts);
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Bạn đã thử đăng ký quá nhiều lần. Vui lòng thử lại sau {RateLimitWindow.TotalMinutes} phút."
                };
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && !u.IsDeleted))
            {
                await _rateLimitService.IncrementAttemptAsync(rateLimitKey, RateLimitWindow);
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email đã được sử dụng"
                };
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username && !u.IsDeleted))
            {
                await _rateLimitService.IncrementAttemptAsync(rateLimitKey, RateLimitWindow);
                _logger.LogWarning("Registration attempt with existing username: {Username}", request.Username);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tên đăng nhập đã được sử dụng"
                };
            }

            // Hash password
            var (hash, salt) = _passwordHasher.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                UserID = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                Phone = request.Phone,
                FullName = request.FullName,
                Address = request.Address,
                PasswordHash = hash,
                PasswordSalt = salt,
                PasswordAlgo = "SHA2_512+iter1000",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Users.Add(user);

            // Assign Customer role
            var customerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Customer");

            if (customerRole == null)
            {
                _logger.LogError("Customer role not found in database during registration");
                return new AuthResponse
                {
                    Success = false,
                    Message = "Hệ thống chưa được cấu hình đầy đủ. Vui lòng liên hệ quản trị viên."
                };
            }

            var userRole = new UserRole
            {
                UserID = user.UserID,
                RoleID = customerRole.RoleID,
                AssignedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);

            await _context.SaveChangesAsync();

            // Reset rate limit on successful registration
            await _rateLimitService.ResetAttemptsAsync(rateLimitKey);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng ký thành công",
                User = MapToUserInfo(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = $"Lỗi khi đăng ký: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // Rate limiting check
            var rateLimitKey = $"login:{request.Email}";
            var isAllowed = await _rateLimitService.IsAllowedAsync(rateLimitKey, MaxLoginAttempts, RateLimitWindow);
            
            if (!isAllowed)
            {
                var attempts = await _rateLimitService.GetAttemptCountAsync(rateLimitKey);
                _logger.LogWarning("Login rate limit exceeded for email: {Email}, attempts: {Attempts}", request.Email, attempts);
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Bạn đã đăng nhập sai quá nhiều lần. Tài khoản đã bị khóa tạm thời trong {AccountLockoutDuration.TotalMinutes} phút."
                };
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

            if (user == null)
            {
                await _rateLimitService.IncrementAttemptAsync(rateLimitKey, RateLimitWindow);
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                };
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                await _rateLimitService.IncrementAttemptAsync(rateLimitKey, RateLimitWindow);
                var remainingAttempts = MaxLoginAttempts - await _rateLimitService.GetAttemptCountAsync(rateLimitKey);
                
                _logger.LogWarning("Login attempt with incorrect password for email: {Email}, remaining attempts: {Remaining}", 
                    request.Email, remainingAttempts);
                
                var message = remainingAttempts > 0
                    ? $"Email hoặc mật khẩu không đúng. Còn {remainingAttempts} lần thử."
                    : "Email hoặc mật khẩu không đúng. Tài khoản đã bị khóa tạm thời.";
                
                return new AuthResponse
                {
                    Success = false,
                    Message = message
                };
            }

            // Reset rate limit on successful login
            await _rateLimitService.ResetAttemptsAsync(rateLimitKey);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                User = MapToUserInfo(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = $"Lỗi khi đăng nhập: {ex.Message}"
            };
        }
    }

    public async Task<(bool success, string message)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("Change password attempt for non-existent user: {UserId}", userId);
                return (false, "Người dùng không tồn tại");
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Change password attempt with incorrect current password for user: {UserId}", userId);
                return (false, "Mật khẩu hiện tại không đúng");
            }

            // Check if new password is the same as current password
            if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Change password attempt with same password as current for user: {UserId}", userId);
                return (false, "Mật khẩu không thay đổi, vui lòng nhập lại");
            }

            // Hash new password
            var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);

            // Update password
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

            return (true, "Đổi mật khẩu thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return (false, $"Lỗi khi đổi mật khẩu: {ex.Message}");
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserID == userId && !u.IsDeleted);
    }

    public UserInfo MapToUserInfo(User user)
    {
        return new UserInfo
        {
            UserId = user.UserID,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        };
    }
}
