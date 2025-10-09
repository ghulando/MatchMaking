using MatchMaking.Domain.Repositories;
using MatchMaking.Domain.Entities;

namespace MatchMaking.Application.UseCases;

public class ProcessMatchRequestUseCase
{
    private readonly IPendingPlayersRepository _pendingPlayersRepository;
    private readonly int _playersPerMatch;

    public ProcessMatchRequestUseCase(
        IPendingPlayersRepository pendingPlayersRepository,
        int playersPerMatch = 3)
    {
        _pendingPlayersRepository = pendingPlayersRepository;
        _playersPerMatch = playersPerMatch;
    }

    public async Task<bool> ExecuteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        // Add player to pending queue
        var added = await _pendingPlayersRepository.AddPlayerAsync(userId);
        
        if (!added)
            return false; // Player already in queue

        // Check if we have enough players
        var pendingCount = await _pendingPlayersRepository.GetPendingCountAsync();
        return pendingCount >= _playersPerMatch;
    }
}
