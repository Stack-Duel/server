using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.Languages;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.Problems;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestSuites;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Infrastructure.ExecutionEngine;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.TestCaseGeneration;

internal sealed class TestCaseGenerationService(
    IProblemRepository problemRepository,
    ITestSuiteWriteRepository testSuiteRepository,
    ILanguageReadRepository languageReadRepository,
    ICodeTemplateStrategyResolver templateResolver,
    ITestCaseGeneratorService generator,
    Judge0ReferenceSolutionRunner runner,
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

        // 1. A reference solution that can't pass the curated cases must not be trusted to
        //    generate ground truth for random ones.
        IReadOnlyList<Guid> curatedTestCaseIds = await testSuiteRepository.FindTestCaseIdsByProblemSetupIdAsync(
            request.ProblemSetupId,
            cancellationToken
        );

        List<TestCase> curatedTestCases = await db.Set<TestCase>()
            .AsNoTracking()
            .Include(tc => tc.Inputs.OrderBy(i => i.Position))
            .Include(tc => tc.ExpectedOutputs)
            .Where(tc => curatedTestCaseIds.Contains(tc.Id) && tc.Source == TestCaseSource.Authored)
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

        // 2. Reference solution is trusted — persist the spec and attach it to the setup.
        // Everything from here on runs in one transaction: a failure partway through (a
        // transient Judge0 error, say) must not leave the setup pointing at a spec with no
        // generated cases behind it — that would poison it, since the idempotency check above
        // only looks at whether a spec is attached, not whether generation actually finished.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        int previousVersion = await db
            .Set<TestCaseGenerationSpec>()
            .Where(s => s.ProblemSetupId == request.ProblemSetupId)
            .Select(s => s.Version)
            .DefaultIfEmpty(0)
            .MaxAsync(cancellationToken);

        var spec = new TestCaseGenerationSpec(
            request.ProblemSetupId,
            request.Parameters,
            request.OutputValueType,
            request.TargetCaseCount,
            request.Seed,
            version: previousVersion + 1
        );

        // Stage the spec and the problem's scalar changes without saving yet — problem is
        // already tracked (loaded via FindBySetupIdAsync, not AsNoTracking), so calling
        // IProblemRepository.UpdateAsync here would re-run context.Problems.Update(), forcing
        // the whole graph (including the m2m TestSuites join) to Modified and misfiring
        // against rows that either don't exist yet or don't need touching. Everything gets
        // saved together in one SaveChangesAsync call once the generated cases are built below.
        db.Set<TestCaseGenerationSpec>().Add(spec);

        problem.SetReferenceSolution(request.ProblemSetupId, request.ReferenceSolutionCode);
        problem.SetGenerationSpecId(request.ProblemSetupId, spec.Id);

        // 3. Generate inputs and run the reference solution against them in rounds, until
        // TargetCaseCount inputs have actually succeeded (not just been attempted) — some
        // specs (Two Sum's, notably) can produce unsolvable random instances, so "attempt N"
        // and "succeed N times" are different things, and callers want the latter.
        // MaxAttemptMultiplier bounds runaway looping if a spec can basically never succeed.
        const int MaxAttemptMultiplier = 15;
        int maxAttempts = request.TargetCaseCount * MaxAttemptMultiplier;
        int batchSize = Math.Max(request.TargetCaseCount, 1);

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
                            .Select(v => new CodeTemplateInput(v.Value, v.ValueType))
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

                // Normalize through JSON so the persisted value matches the compact convention
                // curated cases use (e.g. Python's "[0, 1]" -> "[0,1]") regardless of which
                // language the reference solution happens to be written in.
                actualOutput = Judge0ReferenceSolutionRunner.TryNormalizeJson(actualOutput) ?? actualOutput;

                successfulCases.Add((roundCases[i].Index, actualOutput));
            }
        }

        // 4. Retire the problem's previous generated pool and add the newly verified cases —
        // retired rather than deleted, since a past submission may already reference one of them.
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
            // TestSuite.TestCases is a PropertyAccessMode.Field-backed collection — EF's
            // standard change detection doesn't reliably pick up additions to it via graph
            // traversal alone (the same limitation the seeders work around with raw SQL), so
            // each newly created child is marked Added explicitly, mirroring the approach
            // EvaluateStepHandler already uses for this codebase's field-backed collections.
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

    private static GenerateTestCasesResult Failed(GenerateTestCasesRequest request, string error) =>
        new(request.TargetCaseCount, 0, 0, [error]);
}