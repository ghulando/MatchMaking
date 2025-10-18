using MatchMaking.Application.DTOs;
using MatchMaking.Domain.Repositories;

namespace MatchMaking.Application.UseCases;

public class GetMatchInfoUseCase(IMatchRepository matchRepository)
{
    private readonly IMatchRepository _matchRepository = matchRepository;

    public async Task<MatchInfoDto?> ExecuteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        var match = await _matchRepository.GetMatchByUserIdAsync(userId);
        
        if (match == null)
            return null;

        return new MatchInfoDto(match.MatchId, match.UserIds);
    }
}
