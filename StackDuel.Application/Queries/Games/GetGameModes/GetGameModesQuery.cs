using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Games.GetGameModes;

public sealed record GetGameModesQuery : IQuery<IReadOnlyList<GameModeDto>>;