using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemById;

public sealed record GetProblemByIdQuery(Guid Id, Guid? GameId = null, Guid? RequestedByUserId = null)
    : IQuery<ProblemWithSetupsDto>;