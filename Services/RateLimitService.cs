using Microsoft.Extensions.Caching.Memory;

namespace TourViet.Services;

public class RateLimitService : IRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(IMemoryCache cache, ILogger<RateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window)
    {
        var cacheKey = $"ratelimit:{key}";
        var attempts = _cache.Get<int?>(cacheKey) ?? 0;
        
        return Task.FromResult(attempts < maxAttempts);
    }

    public Task<int> GetAttemptCountAsync(string key)
    {
        var cacheKey = $"ratelimit:{key}";
        var attempts = _cache.Get<int?>(cacheKey) ?? 0;
        
        return Task.FromResult(attempts);
    }

    public Task IncrementAttemptAsync(string key, TimeSpan window)
    {
        var cacheKey = $"ratelimit:{key}";
        var attempts = _cache.Get<int?>(cacheKey) ?? 0;
        attempts++;

        _cache.Set(cacheKey, attempts, window);
        
        _logger.LogWarning("Rate limit incremented for key: {Key}, attempts: {Attempts}", key, attempts);
        
        return Task.CompletedTask;
    }

    public Task ResetAttemptsAsync(string key)
    {
        var cacheKey = $"ratelimit:{key}";
        _cache.Remove(cacheKey);
        
        return Task.CompletedTask;
    }
}

