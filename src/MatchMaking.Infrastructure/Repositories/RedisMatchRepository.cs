using System.Text.Json;
using MatchMaking.Domain.Entities;
using MatchMaking.Domain.Repositories;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Repositories;

public class RedisMatchRepository : IMatchRepository
{
    private readonly IDatabase _redis;

    public RedisMatchRepository(IDatabase redis)
    {
        _redis = redis;
    }

    public async Task<Match?> GetMatchByUserIdAsync(string userId)
    {
        var key = $"user_match:{userId}";
        var matchData = await _redis.StringGetAsync(key);
        
        if (!matchData.HasValue)
            return null;

        var data = JsonSerializer.Deserialize<MatchData>(matchData!);
        if (data == null)
            return null;

        return new Match(data.MatchId, data.UserIds);
    }

    public async Task StoreMatchAsync(Match match)
    {
        var matchData = new MatchData(match.MatchId, match.UserIds);
        var serialized = JsonSerializer.Serialize(matchData);

        // Store match info for each user
        var tasks = match.UserIds.Select(async userId =>
        {
            var key = $"user_match:{userId}";
            await _redis.StringSetAsync(key, serialized, TimeSpan.FromHours(24));
        });

        await Task.WhenAll(tasks);
    }

    private record MatchData(string MatchId, string[] UserIds);
}
