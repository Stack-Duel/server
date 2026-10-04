using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Games.GetGameState;

public sealed record GetGameStateQuery(Guid GameId, Guid RequestedByUserId) : IQuery<GameStateDto>;