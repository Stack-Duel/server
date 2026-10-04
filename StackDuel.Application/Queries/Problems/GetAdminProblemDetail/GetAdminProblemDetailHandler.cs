using Ardalis.Result;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Queries.Problems.GetAdminProblemDetail;

internal sealed class GetAdminProblemDetailHandler(
    IProblemReadRepository problemReadRepository,
    ILanguageReadRepository languageReadRepository,
    IProblemPoolRepository problemPoolRepository,
    ITrackReadRepository trackReadRepository
) : IQueryHandler<GetAdminProblemDetailQuery, AdminProblemDetailDto>
{
    public async Task<Result<AdminProblemDetailDto>> Handle(
        GetAdminProblemDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var problem = await problemReadRepository.FindByIdForAdminAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        var languages = await languageReadRepository.FindLanguagesByVersionId(
            problem.AvailableLanguageVersionIds(),
            cancellationToken
        );
        var poolKeys = await problemPoolRepository.GetPoolKeysForProblemAsync(problem.Id, cancellationToken);

        Track? track = problem.TrackId is Guid trackId
            ? await trackReadRepository.FindByIdAsync(trackId, cancellationToken)
            : null;

        Dictionary<Guid, (string LanguageName, string VersionLabel)> versionLookup = languages
            .SelectMany(language =>
                language.Versions.Select(version =>
                    (version.Id, LanguageName: (string)language.Name, VersionLabel: (string)version.Version)
                )
            )
            .ToDictionary(x => x.Id, x => (x.LanguageName, x.VersionLabel));

        var setups = problem
            .Setups.Select(setup =>
            {
                var (languageName, versionLabel) = versionLookup.TryGetValue(setup.LanguageVersionId, out var found)
                    ? found
                    : ("Unknown", "?");

                List<AdminSampleTestCaseDto> sampleTestCases =
                [
                    .. setup
                        .TestSuites.Where(ts => ts.Type == TestSuiteType.Sample)
                        .SelectMany(ts => ts.TestCases)
                        .Where(tc => tc.Source == TestCaseSource.Authored && tc.RetiredAt is null)
                        .Select(tc => new AdminSampleTestCaseDto(
                            tc.Id,
                            tc.Name,
                            [.. tc.Inputs.Select(i => new AdminSampleTestCaseInputDto(i.Value, i.ValueType))],
                            tc.ExpectedOutputs.Select(o => o.Value).FirstOrDefault() ?? string.Empty,
                            tc.ExpectedOutputs.Select(o => o.ValueType).FirstOrDefault() ?? string.Empty
                        )),
                ];

                return new AdminProblemSetupDto(
                    setup.Id,
                    setup.LanguageVersionId,
                    languageName,
                    versionLabel,
                    setup.FunctionName,
                    setup.InitialCode,
                    setup.ReferenceSolutionCode,
                    setup.ReferenceSolutionCode is not null,
                    setup.GenerationSpecId is not null,
                    setup.TestSuites.Count,
                    setup.TestSuites.Sum(ts => ts.TestCases.Count),
                    sampleTestCases
                );
            })
            .ToList();

        return Result.Success(
            new AdminProblemDetailDto(
                problem.Id,
                problem.Slug,
                problem.Title,
                problem.Question,
                problem.Difficulty.Value,
                problem.Difficulty.Tier,
                problem.TimeLimit.Milliseconds,
                problem.MemoryLimit.Megabytes,
                problem.Status,
                problem.ValidationFailureReason,
                problem.TrackId,
                track?.Name,
                problem.CreatedAt,
                problem.CreatedBy?.Username.Value,
                [.. problem.Tags.Select(t => t.Name.Value)],
                poolKeys,
                setups,
                problem.GenerationSpec?.Parameters ?? [],
                problem.GenerationSpec?.OutputValueType,
                problem.GenerationSpec?.TargetCaseCount,
                problem.GenerationSpec?.Seed
            )
        );
    }
}