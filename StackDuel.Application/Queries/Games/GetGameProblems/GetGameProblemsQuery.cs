using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Games.GetGameProblems;

internal sealed record GetGameProblemsQuery(Guid GameId, Guid RequestedByUserId)
    : IQuery<IReadOnlyList<GameProblemHistoryDto>>;