using System.Text.Json;
using Confluent.Kafka;
using StackExchange.Redis;
using MatchMaking.Worker.Models;

namespace MatchMaking.Worker.Services;

public class MatchmakingWorkerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _producer;
    private readonly IDatabase _redis;
    private readonly ILogger<MatchmakingWorkerService> _logger;
    private readonly int _playersPerMatch;
    private readonly string _pendingPlayersKey = "pending_players";

    public MatchmakingWorkerService(IConfiguration configuration, ILogger<MatchmakingWorkerService> logger)
    {
        _logger = logger;
        _playersPerMatch = configuration.GetValue<int>("MatchMaking:PlayersPerMatch", 3);

        // Initialize Kafka consumer
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe("matchmaking.request");

        // Initialize Kafka producer
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            ClientId = Environment.MachineName
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();

        // Initialize Redis connection
        var redisConnectionString = configuration["Redis:ConnectionString"];
        var redis = ConnectionMultiplexer.Connect(redisConnectionString!);
        _redis = redis.GetDatabase();

        _logger.LogInformation("MatchmakingWorkerService initialized with {PlayersPerMatch} players per match", 
            _playersPerMatch);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MatchmakingWorkerService started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (result?.Message?.Value != null)
                    {
                        await ProcessMatchRequest(result.Message.Value);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from matchmaking.request topic");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in worker service");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MatchmakingWorkerService is shutting down");
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessMatchRequest(string messageValue)
    {
        try
        {
            var matchRequest = JsonSerializer.Deserialize<MatchRequest>(messageValue);
            
            if (matchRequest?.UserId == null)
            {
                _logger.LogWarning("Invalid match request received: {Message}", messageValue);
                return;
            }

            _logger.LogInformation("Processing match request for user {UserId}", matchRequest.UserId);

            // Add user to pending players set (atomic operation)
            var playersAdded = await _redis.SetAddAsync(_pendingPlayersKey, matchRequest.UserId);
            
            if (!playersAdded)
            {
                _logger.LogInformation("User {UserId} already in pending players queue", matchRequest.UserId);
                return;
            }

            // Check if we have enough players for a match
            var pendingCount = await _redis.SetLengthAsync(_pendingPlayersKey);
            
            if (pendingCount >= _playersPerMatch)
            {
                await CreateMatch();
            }
            else
            {
                _logger.LogInformation("Added user {UserId} to pending queue. Current count: {Count}/{Required}", 
                    matchRequest.UserId, pendingCount, _playersPerMatch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing match request: {Message}", messageValue);
        }
    }

    private async Task CreateMatch()
    {
        try
        {
            // Atomically get players for the match using Lua script to ensure consistency
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
                new RedisValue[] { _playersPerMatch });

            var players = (RedisValue[])result!;
            
            if (players.Length < _playersPerMatch)
            {
                _logger.LogInformation("Not enough players available for match creation");
                return;
            }

            // Create match
            var matchId = Guid.NewGuid().ToString();
            var userIds = players.Select(p => (string)p!).ToArray();
            
            var matchComplete = new MatchComplete(matchId, userIds);
            var message = JsonSerializer.Serialize(matchComplete);
            
            // Send match completion message
            await _producer.ProduceAsync("matchmaking.complete", 
                new Message<string, string> 
                { 
                    Key = matchId, 
                    Value = message 
                });

            _logger.LogInformation("Match created successfully! MatchId: {MatchId}, Players: {Players}", 
                matchId, string.Join(", ", userIds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        _producer?.Dispose();
        base.Dispose();
    }
}