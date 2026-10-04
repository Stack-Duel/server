using StackDuel.Application.Games.Dtos;

namespace StackDuel.Application.Queries.Games.GetMyActiveGames;

internal sealed record GetMyActiveGamesQuery(Guid RequestedByUserId) : IQuery<IReadOnlyList<MyActiveGameDto>>;