namespace MatchMaking.Domain.Repositories;

public interface IMessagePublisher
{
    Task PublishMatchRequestAsync(string userId);
    Task PublishMatchCompleteAsync(string matchId, string[] userIds);
}
