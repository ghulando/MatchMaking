using MatchMaking.Service.Models;

namespace MatchMaking.Service.Services;

public interface IMatchService
{
    Task<bool> CanMakeRequestAsync(string userId);

    Task RequestMatchAsync(string userId);

    Task<MatchInfo?> GetMatchInfoAsync(string userId);
    
    Task StoreMatchInfoAsync(string matchId, string[] userIds);
}