namespace StackDuel.Api.Requests.Leaderboard;

public sealed record GetMyLeaderboardEntryRequest(string GameModeKey, int TimeLimitInSeconds);