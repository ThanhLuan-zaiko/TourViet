namespace TourViet.Services;

public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window);
    Task<int> GetAttemptCountAsync(string key);
    Task IncrementAttemptAsync(string key, TimeSpan window);
    Task ResetAttemptsAsync(string key);
}

