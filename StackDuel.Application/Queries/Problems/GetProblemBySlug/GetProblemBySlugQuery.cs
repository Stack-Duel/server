using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemBySlug;

public sealed record GetProblemBySlugQuery(string Slug) : IQuery<ProblemWithSetupsDto>;