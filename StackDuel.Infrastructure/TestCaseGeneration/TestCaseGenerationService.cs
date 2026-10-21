using Microsoft.EntityFrameworkCore;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.Languages;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Application.ValueTypes;
using StackDuel.Domain.Problems;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.ExecutionEngine;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.TestCaseGeneration;

internal sealed class TestCaseGenerationService(
    IProblemRepository problemRepository,
    ITestSuiteWriteRepository testSuiteRepository,
    ILanguageReadRepository languageReadRepository,
    ICodeTemplateStrategyResolver templateResolver,
    ITestCaseGeneratorService generator,
    Judge0ReferenceSolutionRunner runner,
    IValueTypeSpecResolver valueTypeSpecResolver,
    StackDuelDbContext db
) : ITestCaseGenerationService
{
    public async Task<GenerateTestCasesResult> GenerateAsync(
        GenerateTestCasesRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var problem = await problemRepository.FindBySetupIdAsync(request.ProblemSetupId, cancellationToken);
        if (problem is null)
            return Failed(request, $"Problem for setup {request.ProblemSetupId} not found.");

        var setup = problem.Setups.FirstOrDefault(s => s.Id == request.ProblemSetupId);
        if (setup is null)
            return Failed(request, $"Setup {request.ProblemSetupId} not found on problem.");

        var languages = await languageReadRepository.FindLanguagesByVersionId(
            [setup.LanguageVersionId],
            cancellationToken
        );
        var language = languages.FirstOrDefault();
        if (language is null)
            return Failed(request, $"Language for version {setup.LanguageVersionId} not found.");

        var versionEntry = language.Versions.FirstOrDefault(v => v.Id == setup.LanguageVersionId);
        if (versionEntry is null)
            return Failed(request, $"Language version entry {setup.LanguageVersionId} not found.");

        var templateStrategy = templateResolver.Resolve(language.Name.Value);
        int judge0LanguageId = versionEntry.Judge0Id.Value;

        IReadOnlyList<Guid> curatedTestCaseIds = await testSuiteRepository.FindTestCaseIdsByProblemSetupIdAsync(
            request.ProblemSetupId,
            cancellationToken
        );

        List<TestCase> curatedTestCases = await db.Set<TestCase>()
            .AsNoTracking()
            .Include(tc => tc.Inputs.OrderBy(i => i.Position))
            .Include(tc => tc.ExpectedOutputs)
            .Where(tc => curatedTestCaseIds.Contains(tc.Id) && tc.Source == TestCaseSource.Authored)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        if (curatedTestCases.Count > 0)
        {
            List<string> sanityWarnings = await runner.RunAgainstCasesAsync(
                request.ReferenceSolutionCode,
                setup.FunctionName,
                templateStrategy,
                judge0LanguageId,
                curatedTestCases,
                cancellationToken
            );

            if (sanityWarnings.Count > 0)
                return new GenerateTestCasesResult(request.TargetCaseCount, 0, 0, sanityWarnings);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        int previousVersion = await db
            .Set<TestCaseGenerationSpec>()
            .Where(s => s.ProblemSetupId == request.ProblemSetupId)
            .Select(s => s.Version)
            .DefaultIfEmpty(0)
            .MaxAsync(cancellationToken);

        IReadOnlyList<GenerationParameterSpec> snapshotParameters = await SnapshotValueTypes(
            request.Parameters,
            cancellationToken
        );

        var spec = new TestCaseGenerationSpec(
            request.ProblemSetupId,
            snapshotParameters,
            request.OutputValueType,
            request.TargetCaseCount,
            request.Seed,
            version: previousVersion + 1
        );

        db.Set<TestCaseGenerationSpec>().Add(spec);

        problem.SetReferenceSolution(request.ProblemSetupId, request.ReferenceSolutionCode);
        problem.SetGenerationSpecId(request.ProblemSetupId, spec.Id);

        const int MaxAttemptMultiplier = 15;
        int maxAttempts = request.TargetCaseCount * MaxAttemptMultiplier;
        int batchSize = Math.Max(request.TargetCaseCount, 1);

        IReadOnlyDictionary<string, ValueTypeSpec> specsByKey = await valueTypeSpecResolver.ResolveAsync(
            spec.Parameters.Select(p => p.ValueType).Distinct(),
            cancellationToken
        );

        List<(int Index, string Output)> successfulCases = [];
        List<string> warnings = [];
        int nextIndex = 0;
        int attempted = 0;

        while (successfulCases.Count < request.TargetCaseCount && attempted < maxAttempts)
        {
            int roundSize = Math.Min(batchSize, maxAttempts - attempted);
            List<int> roundIndices = [.. Enumerable.Range(nextIndex, roundSize)];
            nextIndex += roundSize;
            attempted += roundSize;

            var roundCases = roundIndices
                .Select(index =>
                    (
                        Index: index,
                        Inputs: generator
                            .GenerateInputs(spec, index)
                            .Select(v => new CodeTemplateInput(v.Value, v.ValueType, specsByKey.GetValueOrDefault(v.ValueType)))
                            .ToList()
                    )
                )
                .ToList();

            List<ExecutionEngineSubmission> submissions =
            [
                .. roundCases.Select(c => new ExecutionEngineSubmission(
                    SourceCode: templateStrategy.Render(
                        new CodeTemplateContext(request.ReferenceSolutionCode, setup.FunctionName, c.Inputs)
                    ),
                    LanguageId: judge0LanguageId,
                    Stdin: templateStrategy.BuildStdin(c.Inputs),
                    TimeLimitMs: null,
                    MemoryLimitKb: null
                )),
            ];

            List<ExecutionEngineResult> results = await runner.SubmitAndPollAsync(submissions, cancellationToken);

            for (int i = 0; i < roundCases.Count && successfulCases.Count < request.TargetCaseCount; i++)
            {
                ExecutionEngineResult? result = results.Count > i ? results[i] : null;

                if (result is null || result.Status != ExecutionEngineResultStatus.Accepted)
                {
                    warnings.Add(
                        $"Case {roundCases[i].Index}: reference solution {result?.Status.ToString() ?? "did not return a result"}."
                    );
                    continue;
                }

                string? actualOutput = Judge0ReferenceSolutionRunner.ParseActualOutput(result.Stdout);
                if (actualOutput is null)
                {
                    warnings.Add($"Case {roundCases[i].Index}: reference solution produced no output.");
                    continue;
                }

                actualOutput = Judge0ReferenceSolutionRunner.TryNormalizeJson(actualOutput) ?? actualOutput;

                successfulCases.Add((roundCases[i].Index, actualOutput));
            }
        }

        string suiteName = $"{problem.Slug.Value} - Generated Cases";
        Guid suiteId = await testSuiteRepository.FindOrCreateGeneratedSuiteAsync(
            suiteName,
            [.. problem.Setups.Select(s => s.Id)],
            cancellationToken
        );

        await testSuiteRepository.RetireGeneratedTestCasesAsync(suiteId, cancellationToken);

        TestSuite suite =
            await testSuiteRepository.FindByIdAsync(suiteId, cancellationToken)
            ?? throw new InvalidOperationException($"Generated suite {suiteId} could not be reloaded.");

        foreach (var (index, output) in successfulCases)
        {
            TestCase testCase = suite.AddGeneratedTestCase($"Generated {index}", spec.Id, index);
            db.Entry(testCase).State = EntityState.Added;

            TestCaseExpectedOutput expectedOutput = testCase.AddExpectedOutput(output, request.OutputValueType);
            db.Entry(expectedOutput).State = EntityState.Added;
        }

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new GenerateTestCasesResult(
            request.TargetCaseCount,
            successfulCases.Count,
            attempted - successfulCases.Count,
            warnings
        );
    }

    private async Task<IReadOnlyList<GenerationParameterSpec>> SnapshotValueTypes(
        IReadOnlyList<GenerationParameterSpec> parameters,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyDictionary<string, ValueTypeSpec> resolved = await valueTypeSpecResolver.ResolveAsync(
            parameters.Select(parameter => parameter.ValueType).Distinct(),
            cancellationToken
        );

        return
        [
            .. parameters.Select(parameter =>
                parameter.ResolvedType is null && resolved.TryGetValue(parameter.ValueType, out ValueTypeSpec? structure)
                    ? parameter with { ResolvedType = structure }
                    : parameter
            ),
        ];
    }

    private static GenerateTestCasesResult Failed(GenerateTestCasesRequest request, string error) =>
        new(request.TargetCaseCount, 0, 0, [error]);
}