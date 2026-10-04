using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemSetup;

public sealed record GetProblemSetupQuery(string Slug, Guid LanguageVersionId) : IQuery<ProblemSetupDto>;