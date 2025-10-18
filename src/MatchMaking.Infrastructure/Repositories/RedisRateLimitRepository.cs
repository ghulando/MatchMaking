using MatchMaking.Domain.Repositories;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Repositories;

public class RedisRateLimitRepository(IDatabase redis) : IRateLimitRepository
{
    private readonly IDatabase _redis = redis;

    public async Task<bool> CanMakeRequestAsync(string userId, int windowMs)
    {
        var key = $"rate_limit:{userId}";
        var lastRequestTime = await _redis.StringGetAsync(key);
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        if (lastRequestTime.HasValue)
        {
            var timeSinceLastRequest = now - (long)lastRequestTime;
            if (timeSinceLastRequest < windowMs)
                return false;
        }

        // Update last request time
        await _redis.StringSetAsync(key, now, TimeSpan.FromMinutes(1));
        return true;
    }
}