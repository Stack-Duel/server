using StackDuel.Application.Problems.RequiredLanguages.Dtos;

namespace StackDuel.Application.Queries.Problems.GetRequiredProblemLanguages;

public sealed record GetRequiredProblemLanguagesQuery : IQuery<IReadOnlyList<RequiredProblemLanguageAdminDto>>;