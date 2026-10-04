using StackDuel.Application.Games.Dtos;

namespace StackDuel.Application.Queries.Games.GetAdminGamePlayerHistory;

public sealed record GetAdminGamePlayerHistoryQuery(Guid GameId) : IQuery<IReadOnlyList<AdminGamePlayerHistoryDto>>;