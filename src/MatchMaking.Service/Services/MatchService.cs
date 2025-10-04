using System.Text.Json;
using Confluent.Kafka;
using StackExchange.Redis;
using MatchMaking.Service.Models;

namespace MatchMaking.Service.Services;

public class MatchService : IMatchService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IDatabase _redis;
    private readonly ILogger<MatchService> _logger;
    private readonly int _rateLimitWindowMs;

    public MatchService(IConfiguration configuration, ILogger<MatchService> logger)
    {
        _logger = logger;
        _rateLimitWindowMs = configuration.GetValue("RateLimit:WindowMs", 100);

        // Initialize Kafka producer
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            ClientId = "matchmaking-service"
        };
        _producer = new ProducerBuilder<string, string>(kafkaConfig).Build();

        // Initialize Redis connection
        var redisConnectionString = configuration["Redis:ConnectionString"];
        var redis = ConnectionMultiplexer.Connect(redisConnectionString!);
        _redis = redis.GetDatabase();

        _logger.LogInformation("MatchService initialized successfully");
    }

    public async Task<bool> CanMakeRequestAsync(string userId)
    {
        try
        {
            var key = $"rate_limit:{userId}";
            var lastRequestTime = await _redis.StringGetAsync(key);
            
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            if (lastRequestTime.HasValue)
            {
                var timeSinceLastRequest = now - (long)lastRequestTime;
                if (timeSinceLastRequest < _rateLimitWindowMs)
                {
                    _logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
                    return false;
                }
            }

            // Update last request time
            await _redis.StringSetAsync(key, now, TimeSpan.FromMinutes(1));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
            return false;
        }
    }

    public async Task RequestMatchAsync(string userId)
    {
        try
        {
            var matchRequest = new MatchRequest(userId);
            var message = JsonSerializer.Serialize(matchRequest);
            
            var result = await _producer.ProduceAsync("matchmaking.request", 
                new Message<string, string> 
                { 
                    Key = userId, 
                    Value = message 
                });

            _logger.LogInformation("Match request sent for user {UserId} to partition {Partition}", 
                userId, result.Partition.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending match request for user {UserId}", userId);
            throw;
        }
    }

    public async Task<MatchInfo?> GetMatchInfoAsync(string userId)
    {
        try
        {
            var key = $"user_match:{userId}";
            var matchData = await _redis.StringGetAsync(key);
            
            if (!matchData.HasValue)
            {
                return null;
            }

            var matchInfo = JsonSerializer.Deserialize<MatchInfo>(matchData!);
            _logger.LogInformation("Retrieved match info for user {UserId}: {MatchId}", 
                userId, matchInfo?.MatchId);
            
            return matchInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving match info for user {UserId}", userId);
            return null;
        }
    }

    public async Task StoreMatchInfoAsync(string matchId, string[] userIds)
    {
        try
        {
            var matchInfo = new MatchInfo(matchId, userIds);
            var matchData = JsonSerializer.Serialize(matchInfo);

            // Store match info for each user
            var tasks = userIds.Select(async userId =>
            {
                var key = $"user_match:{userId}";
                await _redis.StringSetAsync(key, matchData, TimeSpan.FromHours(24));
            });

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Stored match info for match {MatchId} with users {UserIds}", 
                matchId, string.Join(", ", userIds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing match info for match {MatchId}", matchId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}