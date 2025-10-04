namespace MatchMaking.Service.Models;

public record MatchInfo(string MatchId, string[] UserIds);

public record MatchRequest(string UserId);

public record MatchComplete(string MatchId, string[] UserIds);