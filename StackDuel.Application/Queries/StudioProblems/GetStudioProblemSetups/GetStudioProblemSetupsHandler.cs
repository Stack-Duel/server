using Ardalis.Result;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemSetups;

internal sealed class GetStudioProblemSetupsHandler(
    IProblemReadRepository problemReadRepository,
    ILanguageReadRepository languageReadRepository
) : IQueryHandler<GetStudioProblemSetupsQuery, IEnumerable<StudioProblemDetailsSetupDto>>
{
    public async Task<Result<IEnumerable<StudioProblemDetailsSetupDto>>> Handle(
        GetStudioProblemSetupsQuery request,
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

        Dictionary<Guid, (Guid LanguageId, string LanguageName, string VersionLabel)> versionLookup = languages
            .SelectMany(language =>
                language.Versions.Select(version =>
                    (
                        version.Id,
                        LanguageId: language.Id,
                        LanguageName: (string)language.Name,
                        VersionLabel: (string)version.Version
                    )
                )
            )
            .ToDictionary(x => x.Id, x => (x.LanguageId, x.LanguageName, x.VersionLabel));

        List<StudioProblemDetailsSetupDto> setups =
        [
            .. problem.ActiveSetups().Select(setup =>
            {
                var (languageId, languageName, versionLabel) = versionLookup.TryGetValue(
                    setup.LanguageVersionId,
                    out var found
                )
                    ? found
                    : (Guid.Empty, "Unknown", "?");

                return new StudioProblemDetailsSetupDto(
                    setup.LanguageVersionId,
                    versionLabel,
                    languageId,
                    languageName,
                    setup.InitialCode,
                    setup.FunctionName ?? string.Empty,
                    setup.ReferenceSolutionCode ?? string.Empty,
                    setup.TestSuites.Count.ToString(),
                    setup.TestSuites.Sum(testSuite => testSuite.TestCases.Count).ToString(),
                    ToSampleTestCases(setup)
                );
            }),
        ];

        return Result<IEnumerable<StudioProblemDetailsSetupDto>>.Success(setups);
    }

    private static IReadOnlyList<StudioProblemSampleTestCaseDto> ToSampleTestCases(ProblemSetup setup) =>
        [
            .. setup
                .TestSuites.Where(testSuite => testSuite.Type == TestSuiteType.Sample)
                .SelectMany(testSuite => testSuite.TestCases)
                .Where(testCase => testCase.Source == TestCaseSource.Authored && testCase.RetiredAt is null)
                .Select(testCase => new StudioProblemSampleTestCaseDto(
                    testCase.Id,
                    testCase.Name,
                    [
                        .. testCase.Inputs.Select(input => new StudioProblemSampleTestCaseInputDto(
                            input.Value,
                            input.ValueType
                        )),
                    ],
                    testCase.ExpectedOutputs.Select(output => output.Value).FirstOrDefault() ?? string.Empty,
                    testCase.ExpectedOutputs.Select(output => output.ValueType).FirstOrDefault() ?? string.Empty
                )),
        ];
}