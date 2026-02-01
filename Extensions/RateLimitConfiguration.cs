using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace TourViet.Extensions
{
    public static class RateLimitConfiguration
    {
        public static IServiceCollection AddAppRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Custom response for rejected requests
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                    await context.HttpContext.Response.WriteAsync("Yêu cầu quá nhanh. Vui lòng chậm lại một chút để bảo vệ hệ thống!", token);
                };

                // 1. Anti-Spam (F5) Policy: Fixed Window
                // Limits each IP to 30 requests per 10 seconds
                options.AddFixedWindowLimiter("AntiSpam", opt =>
                {
                    opt.PermitLimit = 30;
                    opt.Window = TimeSpan.FromSeconds(10);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0; // No queue for spam, just reject
                });

                // 2. Global Concurrency Policy: Concurrency Limiter
                // Limits total concurrent requests to prevent "thundering herd"
                options.AddConcurrencyLimiter("Concurrency", opt =>
                {
                    opt.PermitLimit = 100; // Adjust based on server capacity
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 50; // Queue up to 50 requests when full
                });

                // 3. User-specific Policy (IP based) for Anti-Spam
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 40, // Slightly higher than individual policy as a global safeguard
                            Window = TimeSpan.FromSeconds(15),
                            QueueLimit = 0
                        });
                });
            });

            return services;
        }
    }
}
