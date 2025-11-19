namespace TourViet.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var path = context.Request.Path;
        var method = context.Request.Method;
        var userId = context.Session.GetString("UserId") ?? "Anonymous";

        try
        {
            await _next(context);
            
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            _logger.LogInformation(
                "Request: {Method} {Path} | User: {UserId} | Status: {StatusCode} | Duration: {Duration}ms",
                method, path, userId, statusCode, duration);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogError(ex,
                "Request failed: {Method} {Path} | User: {UserId} | Duration: {Duration}ms",
                method, path, userId, duration);
            
            throw;
        }
    }
}

