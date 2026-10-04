using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemById;

public sealed record GetProblemByIdQuery(Guid Id) : IQuery<ProblemWithSetupsDto>;