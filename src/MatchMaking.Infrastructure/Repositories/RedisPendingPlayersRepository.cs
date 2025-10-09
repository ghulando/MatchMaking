using MatchMaking.Domain.Repositories;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Repositories;

public class RedisPendingPlayersRepository : IPendingPlayersRepository
{
    private readonly IDatabase _redis;
    private readonly string _pendingPlayersKey = "pending_players";

    public RedisPendingPlayersRepository(IDatabase redis)
    {
        _redis = redis;
    }

    public async Task<bool> AddPlayerAsync(string userId)
    {
        return await _redis.SetAddAsync(_pendingPlayersKey, userId);
    }

    public async Task<long> GetPendingCountAsync()
    {
        return await _redis.SetLengthAsync(_pendingPlayersKey);
    }

    public async Task<string[]> PopPlayersAsync(int count)
    {
        // Atomically get players using Lua script to ensure consistency
        var luaScript = @"
            local players = redis.call('SPOP', KEYS[1], ARGV[1])
            if #players >= tonumber(ARGV[1]) then
                return players
            else
                -- Put players back if we didn't get enough
                for i=1,#players do
                    redis.call('SADD', KEYS[1], players[i])
                end
                return {}
            end";

        var result = await _redis.ScriptEvaluateAsync(luaScript, 
            new RedisKey[] { _pendingPlayersKey }, 
            new RedisValue[] { count });

        var players = (RedisValue[])result!;
        return players.Select(p => (string)p!).ToArray();
    }
}
