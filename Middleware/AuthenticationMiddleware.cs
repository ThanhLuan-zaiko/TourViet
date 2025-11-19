using TourViet.Services;

namespace TourViet.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        // Check if user session exists and is valid
        var userIdString = context.Session.GetString("UserId");
        
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            // Verify user still exists and is not deleted
            var user = await authService.GetUserByIdAsync(userId);
            
            if (user == null || user.IsDeleted)
            {
                // User no longer exists or is deleted, clear session
                context.Session.Clear();
                _logger.LogWarning("Session cleared for invalid user: {UserId}", userId);
            }
            else
            {
                // Refresh session data if needed
                if (string.IsNullOrEmpty(context.Session.GetString("FullName")))
                {
                    context.Session.SetString("FullName", user.FullName ?? user.Username);
                }
            }
        }

        await _next(context);
    }
}

