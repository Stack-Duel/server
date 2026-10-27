using Ardalis.Result;
using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Queries.Problems.GetMyProblems;

internal sealed class GetMyProblemsHandler(
    IProblemReadRepository problemReadRepository,
    ILanguageReadRepository languageReadRepository,
    UserContext userContext
) : IQueryHandler<GetMyProblemsQuery, PageResult<MyProblemListItemDto>>
{
    public async Task<Result<PageResult<MyProblemListItemDto>>> Handle(
        GetMyProblemsQuery request,
        CancellationToken cancellationToken
    )
    {
        var page = await problemReadRepository.GetMyProblemsPagedAsync(
            userContext.User!.Id,
            request.Input.Statuses,
            request.Input.PaginationRequest,
            request.Input.Search,
            cancellationToken
        );

        Guid[] versionIds = [.. page.Results.SelectMany(r => r.LanguageVersionIds).Distinct()];

        var languages = await languageReadRepository.FindLanguagesByVersionId(versionIds, cancellationToken);

        Dictionary<Guid, string> languageNameByVersionId = languages
            .SelectMany(language => language.Versions.Select(version => (version.Id, language.Name)))
            .ToDictionary(x => x.Id, x => (string)x.Name);

        var results = page
            .Results.Select(row => new MyProblemListItemDto
            {
                Id = row.Id,
                Slug = row.Slug,
                Title = row.Title,
                DifficultyValue = row.DifficultyValue,
                DifficultyTier = new Difficulty(row.DifficultyValue).Tier,
                Status = row.Status,
                TimeLimitMs = row.TimeLimitMs,
                MemoryLimitMb = row.MemoryLimitMb,
                Tags = row.Tags,
                Languages =
                [
                    .. row
                        .LanguageVersionIds.Select(id => languageNameByVersionId.GetValueOrDefault(id, "Unknown"))
                        .Distinct(),
                ],
                SetupCount = row.SetupCount,
                CreatedAt = row.CreatedAt,
            })
            .ToList();

        return Result.Success(
            new PageResult<MyProblemListItemDto>
            {
                Results = results,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size,
            }
        );
    }
}