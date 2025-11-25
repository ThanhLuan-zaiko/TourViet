namespace TourViet.Middleware;

public class BookingAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BookingAuthorizationMiddleware> _logger;

    public BookingAuthorizationMiddleware(RequestDelegate next, ILogger<BookingAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Only apply to booking-related API endpoints
        if (path.StartsWith("/api/booking"))
        {
            var userId = context.Session.GetString("UserId");
            
            // Check if user is authenticated for booking operations
            if (context.Request.Method == "POST" && path.Contains("/create"))
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized booking attempt from IP: {IP}", context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        success = false, 
                        message = "Bạn cần đăng nhập để đặt tour." 
                    });
                    return;
                }
            }

            // Check admin role for management operations
            if (context.Request.Method == "PUT" || 
                (context.Request.Method == "GET" && path.Contains("/all")))
            {
                var userRoles = context.Session.GetString("Roles")?.Split(',') ?? Array.Empty<string>();
                var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
                
                if (!isAdministrativeStaff)
                {
                    _logger.LogWarning(
                        "Unauthorized admin booking operation attempt by user: {UserId}", 
                        userId);
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        success = false, 
                        message = "Bạn không có quyền thực hiện thao tác này." 
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class BookingAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseBookingAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BookingAuthorizationMiddleware>();
    }
}
