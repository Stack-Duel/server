using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Games.GetGameAccess;

public sealed record GetGameAccessQuery(Guid GameId, Guid RequestedByUserId) : IQuery<GameAccessDto>;