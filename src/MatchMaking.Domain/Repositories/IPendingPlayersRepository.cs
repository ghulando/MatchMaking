using MatchMaking.Domain.Entities;

namespace MatchMaking.Domain.Repositories;

public interface IPendingPlayersRepository
{
    Task<bool> AddPlayerAsync(string userId);
    Task<long> GetPendingCountAsync();
    Task<string[]> PopPlayersAsync(int count);
}
