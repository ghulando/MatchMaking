using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Application.UseCases;

namespace MatchMaking.Worker.Services;

public class MatchmakingWorkerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchmakingWorkerService> _logger;
    private readonly int _playersPerMatch;

    public MatchmakingWorkerService(
        IConfiguration configuration, 
        IServiceProvider serviceProvider,
        ILogger<MatchmakingWorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _playersPerMatch = configuration.GetValue("MatchMaking:PlayersPerMatch", 3);

        // Initialize Kafka consumer
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe("matchmaking.request");

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
            var matchRequest = JsonSerializer.Deserialize<MatchRequestDto>(messageValue);
            
            if (matchRequest?.UserId == null)
            {
                _logger.LogWarning("Invalid match request received: {Message}", messageValue);
                return;
            }

            _logger.LogInformation("Processing match request for user {UserId}", matchRequest.UserId);

            using var scope = _serviceProvider.CreateScope();
            var processMatchRequestUseCase = scope.ServiceProvider.GetRequiredService<ProcessMatchRequestUseCase>();
            var createMatchUseCase = scope.ServiceProvider.GetRequiredService<CreateMatchUseCase>();

            // Process the match request
            var shouldCreateMatch = await processMatchRequestUseCase.ExecuteAsync(matchRequest.UserId);
            
            // If enough players, create a match
            if (shouldCreateMatch)
            {
                var matchId = await createMatchUseCase.ExecuteAsync();
                
                if (matchId != null)
                {
                    _logger.LogInformation("Match created successfully! MatchId: {MatchId}", matchId);
                }
                else
                {
                    _logger.LogInformation("Not enough players available for match creation");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing match request: {Message}", messageValue);
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }

    private record MatchRequestDto(string UserId);
}
