namespace StackDuel.Api.Requests.Leaderboard;

public sealed record GetLeaderboardRequest(string GameModeKey, int TimeLimitInSeconds, int Page, int Size);