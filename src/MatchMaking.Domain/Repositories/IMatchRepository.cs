using MatchMaking.Domain.Entities;

namespace MatchMaking.Domain.Repositories;

public interface IMatchRepository
{
    Task<Match?> GetMatchByUserIdAsync(string userId);
    Task StoreMatchAsync(Match match);
}
