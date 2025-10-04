namespace MatchMaking.Worker.Models;

public record MatchRequest(string UserId);

public record MatchComplete(string MatchId, string[] UserIds);