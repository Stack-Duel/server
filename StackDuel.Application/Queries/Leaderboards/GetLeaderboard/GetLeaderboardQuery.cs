using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;

namespace StackDuel.Application.Queries.Leaderboards.GetLeaderboard;

public sealed record GetLeaderboardQuery(
    string GameModeKey,
    int TimeLimitInSeconds,
    PaginationRequest PaginationRequest,
    Guid? RequestedByUserId
) : IQuery<PageResult<LeaderboardEntryDto>>;