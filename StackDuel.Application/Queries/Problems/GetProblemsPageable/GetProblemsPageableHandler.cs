using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.ValueObjects;
using Ardalis.Result;
using DifficultyTier = StackDuel.Domain.Problems.Enums.DifficultyTier;

namespace StackDuel.Application.Queries.Problems.GetProblemsPageable;

internal sealed class GetProblemsPageableHandler(
    IProblemReadRepository problemReadRepository,
    ILanguageReadRepository languageReadRepository
) : IQueryHandler<GetProblemsPageableQuery, PageResult<ProblemDto>>
{
    public async Task<Result<PageResult<ProblemDto>>> Handle(
        GetProblemsPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        var page = await problemReadRepository.GetPagedAsync(
            request.PaginationRequest,
            request.Search,
            cancellationToken
        );

        Guid[] versionIds = [.. page.Results.SelectMany(r => r.LanguageVersionIds).Distinct()];

        var languages = await languageReadRepository.FindLanguagesByVersionId(versionIds, cancellationToken);

        Dictionary<Guid, ProblemLanguageDto> versionLanguages = languages
            .SelectMany(language =>
                language.Versions.Select(version =>
                    (
                        version.Id,
                        Language: new ProblemLanguageDto(language.Id, language.Name.Value, language.Slug.Value)
                    )
                )
            )
            .ToDictionary(x => x.Id, x => x.Language);

        var results = page
            .Results.Select(row => new ProblemDto(
                row.Id,
                row.Slug,
                row.Title,
                ToDifficultyTier(row.DifficultyValue),
                row.Tags,
                [
                    .. row
                        .LanguageVersionIds.Select(id => versionLanguages.GetValueOrDefault(id))
                        .OfType<ProblemLanguageDto>()
                        .DistinctBy(language => language.Id),
                ]
            ))
            .ToList();

        return Result.Success(
            new PageResult<ProblemDto>
            {
                Results = results,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size,
            }
        );
    }

    private static DifficultyTier ToDifficultyTier(int difficultyValue) =>
        difficultyValue <= Difficulty.BeginnerMax ? DifficultyTier.Beginner
        : difficultyValue <= Difficulty.EasyMax ? DifficultyTier.Easy
        : difficultyValue <= Difficulty.IntermediateMax ? DifficultyTier.Intermediate
        : difficultyValue <= Difficulty.AdvancedMax ? DifficultyTier.Advanced
        : DifficultyTier.Expert;
}