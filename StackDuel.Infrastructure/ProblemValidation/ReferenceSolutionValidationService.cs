using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.Languages;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Infrastructure.ExecutionEngine;

namespace StackDuel.Infrastructure.ProblemValidation;

internal interface IReferenceSolutionValidationService
{
    Task<ReferenceSolutionValidationResult> ValidateAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    );
}

internal sealed record SetupValidationOutcome(
    Guid ProblemSetupId,
    string LanguageLabel,
    bool Passed,
    IReadOnlyList<string> Warnings
);

internal sealed record ReferenceSolutionValidationResult(bool AllPassed, IReadOnlyList<SetupValidationOutcome> Outcomes)
{
    public string BuildFailureSummary() =>
        string.Join(
            "\n",
            Outcomes.Where(o => !o.Passed).Select(o => $"{o.LanguageLabel}: {string.Join("; ", o.Warnings)}")
        );
}

internal sealed class ReferenceSolutionValidationService(
    IProblemRepository problemRepository,
    ILanguageReadRepository languageReadRepository,
    ICodeTemplateStrategyResolver templateResolver,
    Judge0ReferenceSolutionRunner runner
) : IReferenceSolutionValidationService
{
    public async Task<ReferenceSolutionValidationResult> ValidateAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(problemId, cancellationToken);

        if (problem is null)
            return new ReferenceSolutionValidationResult(
                false,
                [new SetupValidationOutcome(Guid.Empty, "problem", false, ["Problem not found."])]
            );

        IEnumerable<Language> languages = await languageReadRepository.FindLanguagesByVersionId(
            [.. problem.Setups.Select(s => s.LanguageVersionId)],
            cancellationToken
        );

        Dictionary<Guid, (Language Language, LanguageVersionEntry Version)> versionLookup = languages
            .SelectMany(language =>
                language.Versions.Select(version => (version.Id, Language: language, Version: version))
            )
            .ToDictionary(x => x.Id, x => (x.Language, x.Version));

        List<SetupValidationOutcome> outcomes = [];

        foreach (ProblemSetup setup in problem.Setups)
        {
            if (!versionLookup.TryGetValue(setup.LanguageVersionId, out var resolved))
            {
                outcomes.Add(
                    new SetupValidationOutcome(
                        setup.Id,
                        setup.LanguageVersionId.ToString(),
                        false,
                        ["Language version not found."]
                    )
                );
                continue;
            }

            string languageLabel = $"{resolved.Language.Name.Value} ({resolved.Version.Version.Value})";

            if (string.IsNullOrWhiteSpace(setup.ReferenceSolutionCode))
            {
                outcomes.Add(
                    new SetupValidationOutcome(setup.Id, languageLabel, false, ["No reference solution provided."])
                );
                continue;
            }

            List<TestCase> sampleTestCases =
            [
                .. setup
                    .TestSuites.Where(ts => ts.Type == TestSuiteType.Sample)
                    .SelectMany(ts => ts.TestCases)
                    .Where(tc => tc.Source == TestCaseSource.Authored && tc.RetiredAt is null),
            ];

            if (sampleTestCases.Count == 0)
            {
                outcomes.Add(
                    new SetupValidationOutcome(
                        setup.Id,
                        languageLabel,
                        false,
                        ["No sample test cases to validate against."]
                    )
                );
                continue;
            }

            ICodeTemplateStrategy templateStrategy = templateResolver.Resolve(resolved.Language.Name.Value);
            int judge0LanguageId = resolved.Version.Judge0Id.Value;

            List<string> warnings = await runner.RunAgainstCasesAsync(
                setup.ReferenceSolutionCode,
                setup.FunctionName,
                templateStrategy,
                judge0LanguageId,
                sampleTestCases,
                cancellationToken
            );

            outcomes.Add(new SetupValidationOutcome(setup.Id, languageLabel, warnings.Count == 0, warnings));
        }

        bool allPassed = outcomes.Count > 0 && outcomes.All(o => o.Passed);
        return new ReferenceSolutionValidationResult(allPassed, outcomes);
    }
}