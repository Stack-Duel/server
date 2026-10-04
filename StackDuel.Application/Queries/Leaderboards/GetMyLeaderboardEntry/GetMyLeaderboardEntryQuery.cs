using StackDuel.Application.Leaderboards.Dtos;

namespace StackDuel.Application.Queries.Leaderboards.GetMyLeaderboardEntry;

public sealed record GetMyLeaderboardEntryQuery(string GameModeKey, int TimeLimitInSeconds, Guid UserId)
    : IQuery<MyLeaderboardEntryDto>;