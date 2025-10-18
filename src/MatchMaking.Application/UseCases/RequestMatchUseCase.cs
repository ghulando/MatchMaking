using MatchMaking.Domain.Repositories;

namespace MatchMaking.Application.UseCases;

public class RequestMatchUseCase(
    IRateLimitRepository rateLimitRepository,
    IMessagePublisher messagePublisher,
    int rateLimitWindowMs = 100)
{
    private readonly IRateLimitRepository _rateLimitRepository = rateLimitRepository;
    private readonly IMessagePublisher _messagePublisher = messagePublisher;
    private readonly int _rateLimitWindowMs = rateLimitWindowMs;

    public async Task<bool> ExecuteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        // Check rate limit
        if (!await _rateLimitRepository.CanMakeRequestAsync(userId, _rateLimitWindowMs))
            return false;

        // Publish match request
        await _messagePublisher.PublishMatchRequestAsync(userId);
        return true;
    }
}
