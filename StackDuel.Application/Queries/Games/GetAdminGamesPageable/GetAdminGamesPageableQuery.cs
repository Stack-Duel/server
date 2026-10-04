using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Queries.Games.GetAdminGamesPageable;

public sealed record GetAdminGamesPageableQuery(GameStatus? Status, PaginationRequest PaginationRequest)
    : IQuery<PageResult<AdminGameListItemDto>>;