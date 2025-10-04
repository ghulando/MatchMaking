using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Service.Models;
using MatchMaking.Service.Services;

namespace MatchMaking.Service;

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
      AutoOffsetReset = AutoOffsetReset.Earliest
    };

    _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    _consumer.Subscribe("matchmaking.complete");
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("MatchCompletionConsumerService started");

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var result = _consumer.Consume(TimeSpan.FromSeconds(1));
                    
          if (result?.Message?.Value != null)
          {
            await ProcessMatchComplete(result.Message.Value);
          }
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Error consuming message from matchmaking.complete topic");
        }
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogInformation("MatchCompletionConsumerService is shutting down");
    }
    finally
    {
      _consumer.Close();
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