using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.ValueObjects;
using Ardalis.Result;
using DifficultyTier = StackDuel.Domain.Problems.Enums.DifficultyTier;

namespace StackDuel.Application.Queries.Problems.GetAdminProblemsPageable;

internal sealed class GetAdminProblemsPageableHandler(
    IProblemReadRepository problemReadRepository,
    ILanguageReadRepository languageReadRepository
) : IQueryHandler<GetAdminProblemsPageableQuery, PageResult<AdminProblemListItemDto>>
{
    public async Task<Result<PageResult<AdminProblemListItemDto>>> Handle(
        GetAdminProblemsPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        var page = await problemReadRepository.GetAdminProblemsPagedAsync(
            request.PaginationRequest,
            request.Search,
            cancellationToken
        );

        Guid[] versionIds = [.. page.Results.SelectMany(r => r.LanguageVersionIds).Distinct()];

        var languages = await languageReadRepository.FindLanguagesByVersionId(versionIds, cancellationToken);

        Dictionary<Guid, string> versionDisplayNames = languages
            .SelectMany(language =>
                language.Versions.Select(version => (version.Id, Display: $"{language.Name} {version.Version}"))
            )
            .ToDictionary(x => x.Id, x => x.Display);

        var results = page
            .Results.Select(row => new AdminProblemListItemDto(
                row.Id,
                row.Slug,
                row.Title,
                row.DifficultyValue,
                ToDifficultyTier(row.DifficultyValue),
                row.Status,
                row.TimeLimitMs,
                row.MemoryLimitMb,
                row.Tags,
                [.. row.LanguageVersionIds.Select(id => versionDisplayNames.GetValueOrDefault(id, "Unknown"))],
                row.SetupCount,
                row.CreatedAt,
                row.CreatedByUsername
            ))
            .ToList();

        return Result.Success(
            new PageResult<AdminProblemListItemDto>
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