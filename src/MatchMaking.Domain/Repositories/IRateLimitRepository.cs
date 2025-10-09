namespace MatchMaking.Domain.Repositories;

public interface IRateLimitRepository
{
    Task<bool> CanMakeRequestAsync(string userId, int windowMs);
}
