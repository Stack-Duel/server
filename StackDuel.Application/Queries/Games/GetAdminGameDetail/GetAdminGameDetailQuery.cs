using StackDuel.Application.Games.Dtos;

namespace StackDuel.Application.Queries.Games.GetAdminGameDetail;

public sealed record GetAdminGameDetailQuery(Guid GameId) : IQuery<GameStateDto>;