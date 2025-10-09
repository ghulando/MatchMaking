using MatchMaking.Domain.Repositories;
using MatchMaking.Domain.Entities;

namespace MatchMaking.Application.UseCases;

public class CreateMatchUseCase
{
    private readonly IPendingPlayersRepository _pendingPlayersRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly int _playersPerMatch;

    public CreateMatchUseCase(
        IPendingPlayersRepository pendingPlayersRepository,
        IMessagePublisher messagePublisher,
        int playersPerMatch = 3)
    {
        _pendingPlayersRepository = pendingPlayersRepository;
        _messagePublisher = messagePublisher;
        _playersPerMatch = playersPerMatch;
    }

    public async Task<string?> ExecuteAsync()
    {
        // Get players from pending queue
        var userIds = await _pendingPlayersRepository.PopPlayersAsync(_playersPerMatch);
        
        if (userIds.Length < _playersPerMatch)
            return null; // Not enough players

        // Create match
        var matchId = Guid.NewGuid().ToString();
        
        // Publish match complete event
        await _messagePublisher.PublishMatchCompleteAsync(matchId, userIds);
        
        return matchId;
    }
}
