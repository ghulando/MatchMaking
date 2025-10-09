namespace MatchMaking.Domain.Entities;

public class MatchRequest
{
    public string UserId { get; private set; }
    public DateTime RequestedAt { get; private set; }

    public MatchRequest(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        UserId = userId;
        RequestedAt = DateTime.UtcNow;
    }
}
