using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Problems.GetProblemBySlug;

internal sealed class GetProblemBySlugHandler(
    IProblemReadRepository problemReadRepository,
    ILanguageReadRepository languageReadRepository
) : IQueryHandler<GetProblemBySlugQuery, ProblemWithSetupsDto>
{
    public async Task<Result<ProblemWithSetupsDto>> Handle(
        GetProblemBySlugQuery request,
        CancellationToken cancellationToken
    )
    {
        var problem = await problemReadRepository.FindBySlugAsync(request.Slug, cancellationToken);

        if (problem is null)
        {
            return Result.NotFound();
        }

        var languages = await languageReadRepository.FindLanguagesByVersionId(
            problem.AvailableLanguageVersionIds(),
            cancellationToken
        );

        var publicTestCases =
            problem
                .Setups.FirstOrDefault()
                ?.PublicTestSuites()
                .SelectMany(suite => suite.TestCases)
                .Select(tc => new PublicTestCaseDto(
                    tc.Name,
                    tc.Description,
                    tc.Inputs.Select(i => new PublicTestCaseInputDto(i.Value, i.ValueType)),
                    tc.ExpectedOutputs.Select(o => new PublicTestCaseExpectedOutputDto(o.Value, o.ValueType))
                ))
            ?? [];

        return Result.Success(
            new ProblemWithSetupsDto(
                Id: problem.Id,
                Slug: problem.Slug,
                Title: problem.Title,
                DifficultyTier: problem.Difficulty.Tier,
                Question: problem.Question,
                AvailableLanguages: languages.Select(language => new ProblemSetupLanguageDto(
                    language.Id,
                    language.Name,
                    Versions: language.Versions.Select(version => new ProblemSetupLanguageVersionDto(
                        version.Id,
                        version.Version
                    ))
                )),
                PublicTestCases: publicTestCases,
                Author: problem.CreatedBy is null
                    ? null
                    : new ProblemAuthorDto(problem.CreatedBy.Username.Value, problem.CreatedBy.ImageUrl?.Value),
                Tags: problem.Tags.Select(t => t.Name.Value)
            )
        );
    }
}