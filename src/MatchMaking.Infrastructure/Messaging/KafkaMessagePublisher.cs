using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Domain.Repositories;

namespace MatchMaking.Infrastructure.Messaging;

public class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaMessagePublisher(IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task PublishMatchRequestAsync(string userId)
    {
        var matchRequest = new { UserId = userId };
        var message = JsonSerializer.Serialize(matchRequest);
        
        await _producer.ProduceAsync("matchmaking.request", 
            new Message<string, string> 
            { 
                Key = userId, 
                Value = message 
            });
    }

    public async Task PublishMatchCompleteAsync(string matchId, string[] userIds)
    {
        var matchComplete = new { MatchId = matchId, UserIds = userIds };
        var message = JsonSerializer.Serialize(matchComplete);
        
        await _producer.ProduceAsync("matchmaking.complete", 
            new Message<string, string> 
            { 
                Key = matchId, 
                Value = message 
            });
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
