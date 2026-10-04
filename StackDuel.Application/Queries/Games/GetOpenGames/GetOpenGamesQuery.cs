using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;

namespace StackDuel.Application.Queries.Games.GetOpenGames;

internal sealed record GetOpenGamesQuery(
    string? GameModeKey,
    PaginationRequest PaginationRequest,
    Guid? RequestedByUserId
) : IQuery<PageResult<GameLobbySummaryDto>>;