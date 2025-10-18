using MatchMaking.Domain.Repositories;
using MatchMaking.Domain.Entities;

namespace MatchMaking.Application.UseCases;

public class StoreMatchInfoUseCase
{
    private readonly IMatchRepository _matchRepository;

    public StoreMatchInfoUseCase(IMatchRepository matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public async Task ExecuteAsync(string matchId, string[] userIds)
    {
        if (string.IsNullOrWhiteSpace(matchId))
            throw new ArgumentException("MatchId cannot be null or empty", nameof(matchId));

        if (userIds == null || userIds.Length == 0)
            throw new ArgumentException("UserIds cannot be null or empty", nameof(userIds));

        var match = new Match(matchId, userIds);
        await _matchRepository.StoreMatchAsync(match);
    }
}
