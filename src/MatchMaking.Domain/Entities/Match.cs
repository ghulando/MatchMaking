namespace MatchMaking.Domain.Entities;

public class Match
{
    public string MatchId { get; private set; }
    public string[] UserIds { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Match(string matchId, string[] userIds)
    {
        if (string.IsNullOrWhiteSpace(matchId))
            throw new ArgumentException("MatchId cannot be null or empty", nameof(matchId));
        
        if (userIds == null || userIds.Length == 0)
            throw new ArgumentException("UserIds cannot be null or empty", nameof(userIds));

        MatchId = matchId;
        UserIds = userIds;
        CreatedAt = DateTime.UtcNow;
    }
}
