using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Service.Models;

namespace MatchMaking.Service.Services;

public class MatchCompletionConsumerService : BackgroundService
{
  private readonly IConsumer<string, string> _consumer;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<MatchCompletionConsumerService> _logger;

  public MatchCompletionConsumerService(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<MatchCompletionConsumerService> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;

    var consumerConfig = new ConsumerConfig
    {
      BootstrapServers = configuration["Kafka:BootstrapServers"],
      GroupId = configuration["Kafka:GroupId"],
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false,
      SessionTimeoutMs = 30000,
      HeartbeatIntervalMs = 10000
    };

    _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("MatchCompletionConsumerService starting...");

    // Wait for Kafka to be ready
    await Task.Delay(15000, stoppingToken); // Wait 15 seconds
    
    try
    {
      _consumer.Subscribe("matchmaking.complete");
      _logger.LogInformation("Subscribed to matchmaking.complete topic");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to subscribe to topic, will retry...");
      await Task.Delay(5000, stoppingToken);
      try
      {
        _consumer.Subscribe("matchmaking.complete");
        _logger.LogInformation("Successfully subscribed to matchmaking.complete topic on retry");
      }
      catch (Exception retryEx)
      {
        _logger.LogError(retryEx, "Failed to subscribe to topic on retry");
        return;
      }
    }

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var result = _consumer.Consume(TimeSpan.FromSeconds(5));
                    
          if (result?.Message?.Value != null)
          {
            await ProcessMatchComplete(result.Message.Value);
            _consumer.Commit(result);
          }
        }
        catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
        {
          _logger.LogWarning("Topic not available yet, waiting...");
          await Task.Delay(5000, stoppingToken);
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Error consuming message from matchmaking.complete topic");
          await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Unexpected error in consumer");
          await Task.Delay(1000, stoppingToken);
        }
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogInformation("MatchCompletionConsumerService is shutting down");
    }
    finally
    {
      try
      {
        _consumer.Close();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error closing consumer");
      }
    }
  }

  private async Task ProcessMatchComplete(string messageValue)
  {
    try
    {
      var matchComplete = JsonSerializer.Deserialize<MatchComplete>(messageValue);
            
      if (matchComplete != null)
      {
        using var scope = _serviceProvider.CreateScope();
        var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
                
        await matchService.StoreMatchInfoAsync(matchComplete.MatchId, matchComplete.UserIds);
                
        _logger.LogInformation("Processed match completion for match {MatchId} with {UserCount} users", 
          matchComplete.MatchId, matchComplete.UserIds.Length);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing match completion message: {Message}", messageValue);
    }
  }

  public override void Dispose()
  {
    _consumer.Dispose();
    base.Dispose();
  }
}