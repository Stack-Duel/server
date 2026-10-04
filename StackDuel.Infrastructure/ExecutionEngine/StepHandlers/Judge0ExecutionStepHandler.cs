using StackDuel.Application.ExecutionAssets;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.Languages;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.ExecutionPipelines.Entities;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Infrastructure.ExecutionEngine.Assert;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StackDuel.Infrastructure.ExecutionEngine.StepHandlers;

internal sealed partial class Judge0ExecutionStepHandler(
    ISubmissionWriteRepository submissionWriteRepository,
    IProblemRepository problemRepository,
    ILanguageReadRepository languageReadRepository,
    IAdditionalFileBundleReadRepository additionalFileBundleReadRepository,
    ICodeTemplateStrategyResolver templateResolver,
    IBatchCodeTemplateStrategyResolver batchTemplateResolver,
    IExecutionEngineStrategy executionEngine,
    ITestCaseGeneratorService testCaseGenerator,
    StackDuelDbContext db
) : IStepHandler
{
    // Covers Vitest/Node startup overhead on top of the per-case budget — a batch submission
    // runs every case in one process, so the outer Judge0 time limit has to fit the whole run,
    // not just one case.
    private const int BatchStartupBufferMs = 5000;

    public bool CanHandle(ExecutionPipelineStepType stepType) => stepType == ExecutionPipelineStepType.Judge0Execute;

    public async Task<StepHandlerResult> ExecuteAsync(StepHandlerContext context)
    {
        var job = context.Job;
        var ct = context.CancellationToken;

        var submission = await submissionWriteRepository.FindByIdAsync(job.SubmissionId, ct);
        if (submission is null)
            return Fail($"Submission {job.SubmissionId} not found.");

        var (problem, setup, setupError) = await ResolveSetupAsync(submission.ProblemSetupId, ct);
        if (setupError is not null)
            return Fail(setupError);

        var languageResult = await ResolveLanguageAsync(setup!.LanguageVersionId, ct);
        if (languageResult.Error is not null)
            return Fail(languageResult.Error);

        var orderedTestCaseIds = submission.Results.Select(r => r.TestCaseId).ToList();
        var (orderedTestCases, testCasesError) = await LoadOrderedTestCasesAsync(orderedTestCaseIds, ct);
        if (testCasesError is not null)
            return Fail(testCasesError);

        var (generationSpecs, generationSpecsError) = await LoadGenerationSpecsAsync(orderedTestCases!, ct);
        if (generationSpecsError is not null)
            return Fail(generationSpecsError);

        if (languageResult.VersionEntry!.ExecutionMode == TestExecutionMode.BatchedTestFramework)
        {
            var batchContext = new BatchExecutionContext(
                problem!,
                setup,
                languageResult.Language!.Name.Value,
                languageResult.VersionEntry.Judge0Id.Value
            );

            return await ExecuteBatchedAsync(
                batchContext,
                orderedTestCases!,
                generationSpecs,
                submission.SourceCode.Value,
                ct
            );
        }

        var templateStrategy = templateResolver.Resolve(languageResult.Language!.Name.Value);

        var (additionalFiles, additionalFilesError) = await ResolveAdditionalFilesAsync(
            setup.AdditionalFileBundleId,
            ct
        );
        if (additionalFilesError is not null)
            return Fail(additionalFilesError);

        var submissions = BuildSubmissions(
            setup,
            orderedTestCases!,
            generationSpecs,
            submission.SourceCode.Value,
            submission.AdditionalFiles,
            templateStrategy,
            languageResult.VersionEntry.Judge0Id.Value,
            additionalFiles
        );

        var results = await executionEngine.SubmitBatchAsync(submissions, job.SubmissionId, ct);

        var tokenMap = BuildTokenMap(orderedTestCases!, results);

        var payload = new ExecutePayload(languageResult.Language.Name.Value, TestExecutionMode.PerCase, tokenMap);

        return new StepHandlerResult(Succeeded: true, ResponsePayload: JsonSerializer.Serialize(payload));
    }

    private async Task<StepHandlerResult> ExecuteBatchedAsync(
        BatchExecutionContext context,
        IReadOnlyList<TestCase> testCases,
        IReadOnlyDictionary<Guid, TestCaseGenerationSpec> generationSpecs,
        string userCode,
        CancellationToken ct
    )
    {
        AssertStepConfiguration? configuredAssert = await FindAssertConfigurationAsync(context.Setup.PipelineId, ct);

        var (batchCases, batchCasesError) = BuildBatchCases(testCases, generationSpecs, configuredAssert);
        if (batchCasesError is not null)
            return Fail(batchCasesError);

        IBatchCodeTemplateStrategy batchStrategy = batchTemplateResolver.Resolve(context.LanguageName);

        int perCaseTimeoutMs = context.Problem.TimeLimit.Milliseconds;
        string source = batchStrategy.RenderBatch(userCode, context.Setup.FunctionName, batchCases!, perCaseTimeoutMs);

        int batchTimeLimitMs = (perCaseTimeoutMs * testCases.Count) + BatchStartupBufferMs;

        var submission = new ExecutionEngineSubmission(
            SourceCode: source,
            LanguageId: context.Judge0LanguageId,
            Stdin: null,
            TimeLimitMs: batchTimeLimitMs,
            MemoryLimitKb: null
        );

        IReadOnlyList<ExecutionEngineResult> results = await executionEngine.SubmitBatchAsync([submission], null, ct);
        string token = results[0].Token;

        var payload = new ExecutePayload(
            context.LanguageName,
            TestExecutionMode.BatchedTestFramework,
            Tokens: [],
            BatchToken: token,
            BatchTestCaseIds: [.. testCases.Select(tc => tc.Id)]
        );

        return new StepHandlerResult(Succeeded: true, ResponsePayload: JsonSerializer.Serialize(payload));
    }

    private sealed record BatchExecutionContext(Problem Problem, ProblemSetup Setup, string LanguageName, int Judge0LanguageId);

    private (IReadOnlyList<BatchTestCase>? Cases, string? Error) BuildBatchCases(
        IReadOnlyList<TestCase> testCases,
        IReadOnlyDictionary<Guid, TestCaseGenerationSpec> generationSpecs,
        AssertStepConfiguration? configuredAssert
    )
    {
        List<BatchTestCase> cases = [];

        foreach (TestCase testCase in testCases)
        {
            TestCaseExpectedOutput? expectedOutput = testCase.ExpectedOutputs.OrderBy(e => e.Id).FirstOrDefault();
            if (expectedOutput is null)
                return (null, $"Test case {testCase.Id} has no expected output.");

            AssertStepConfiguration assertConfig = configuredAssert ?? DefaultAssertConfigFor(expectedOutput.ValueType);

            if (assertConfig.Strategy == AssertStrategy.Regex)
                return (null, "Regex assertions are not supported in batched test execution mode.");

            BatchAssertStrategy batchAssertStrategy = assertConfig.Strategy switch
            {
                AssertStrategy.FloatTolerance => BatchAssertStrategy.FloatTolerance,
                AssertStrategy.SetEquality => BatchAssertStrategy.SetEquality,
                _ => BatchAssertStrategy.ExactMatch,
            };

            IReadOnlyList<CodeTemplateInput> inputs =
                testCase.Source == TestCaseSource.Generated
                    ? testCaseGenerator
                        .GenerateInputs(generationSpecs[testCase.GenerationSpecId!.Value], testCase.GenerationCaseIndex!.Value)
                        .Select(v => new CodeTemplateInput(v.Value, v.ValueType))
                        .ToList()
                    : testCase.Inputs.Select(i => new CodeTemplateInput(i.Value, i.ValueType)).ToList();

            cases.Add(
                new BatchTestCase(
                    testCase.Id,
                    inputs,
                    expectedOutput.Value,
                    batchAssertStrategy,
                    assertConfig.Tolerance,
                    assertConfig.CaseSensitive
                )
            );
        }

        return (cases, null);
    }

    private async Task<AssertStepConfiguration?> FindAssertConfigurationAsync(Guid pipelineId, CancellationToken ct)
    {
        Guid? evaluateStepId = await db
            .Set<ExecutionPipelineStep>()
            .Where(s => EF.Property<Guid>(s, "pipeline_id") == pipelineId && s.StepType == ExecutionPipelineStepType.Evaluate)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (evaluateStepId is null)
            return null;

        return await db.AssertStepConfigurations.FirstOrDefaultAsync(c => c.PipelineStepId == evaluateStepId, ct);
    }

    private static AssertStepConfiguration DefaultAssertConfigFor(string? valueType) =>
        valueType is "double" or "float"
            ? new AssertStepConfiguration { Strategy = AssertStrategy.FloatTolerance, Tolerance = 1e-6m }
            : new AssertStepConfiguration { Strategy = AssertStrategy.SetEquality, CaseSensitive = true };

    private async Task<(Problem? Problem, ProblemSetup? Setup, string? Error)> ResolveSetupAsync(
        Guid setupId,
        CancellationToken ct
    )
    {
        var problem = await problemRepository.FindBySetupIdAsync(setupId, ct);
        if (problem is null)
            return (null, null, $"Problem for setup {setupId} not found.");

        var setup = problem.Setups.FirstOrDefault(s => s.Id == setupId);
        if (setup is null)
            return (null, null, $"Setup {setupId} not found on problem.");

        return (problem, setup, null);
    }

    private async Task<(Language? Language, LanguageVersionEntry? VersionEntry, string? Error)> ResolveLanguageAsync(
        Guid versionId,
        CancellationToken ct
    )
    {
        var languages = await languageReadRepository.FindLanguagesByVersionId([versionId], ct);
        var language = languages.FirstOrDefault();
        if (language is null)
            return (null, null, $"Language for version {versionId} not found.");

        var versionEntry = language.Versions.FirstOrDefault(v => v.Id == versionId);
        if (versionEntry is null)
            return (null, null, $"Language version entry {versionId} not found.");

        return (language, versionEntry, null);
    }

    private async Task<(byte[]? Content, string? Error)> ResolveAdditionalFilesAsync(
        Guid? bundleId,
        CancellationToken ct
    )
    {
        if (bundleId is null)
            return (null, null);

        var bundle = await additionalFileBundleReadRepository.FindByIdAsync(bundleId.Value, ct);
        if (bundle is null)
            return (null, $"Additional file bundle {bundleId} not found.");

        return (bundle.Content, null);
    }

    private async Task<(List<TestCase>? TestCases, string? Error)> LoadOrderedTestCasesAsync(
        List<Guid> orderedTestCaseIds,
        CancellationToken ct
    )
    {
        var testCaseMap = await db.Set<TestCase>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(tc => tc.Inputs.OrderBy(i => i.Position))
            .Include(tc => tc.ExpectedOutputs)
            .Where(tc => orderedTestCaseIds.Contains(tc.Id))
            .ToDictionaryAsync(tc => tc.Id, ct);

        if (testCaseMap.Count != orderedTestCaseIds.Count)
            return (null, "One or more submission test cases could not be loaded.");

        return (orderedTestCaseIds.Select(id => testCaseMap[id]).ToList(), null);
    }

    private async Task<(Dictionary<Guid, TestCaseGenerationSpec> Specs, string? Error)> LoadGenerationSpecsAsync(
        List<TestCase> orderedTestCases,
        CancellationToken ct
    )
    {
        var generationSpecIds = orderedTestCases
            .Where(tc => tc.Source == TestCaseSource.Generated)
            .Select(tc => tc.GenerationSpecId!.Value)
            .Distinct()
            .ToList();

        if (generationSpecIds.Count == 0)
            return ([], null);

        var generationSpecs = await db.Set<TestCaseGenerationSpec>()
            .AsNoTracking()
            .Where(s => generationSpecIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        if (generationSpecs.Count != generationSpecIds.Count)
            return ([], "One or more generated test cases could not be regenerated: their generation spec is missing.");

        return (generationSpecs, null);
    }

    private List<ExecutionEngineSubmission> BuildSubmissions(
        ProblemSetup setup,
        IReadOnlyCollection<TestCase> testCases,
        IReadOnlyDictionary<Guid, TestCaseGenerationSpec> generationSpecs,
        string userCode,
        IReadOnlyList<StackDuel.Domain.Submissions.ValueObjects.SubmissionSourceFile> submittedFiles,
        ICodeTemplateStrategy templateStrategy,
        int judge0LanguageId,
        byte[]? additionalFiles
    )
    {
        var additionalSourceFiles = submittedFiles.Select(f => new CodeTemplateSourceFile(f.Path, f.Content)).ToList();

        return
        [
            .. testCases.Select(testCase =>
            {
                var inputs =
                    testCase.Source == TestCaseSource.Generated
                        ? testCaseGenerator
                            .GenerateInputs(
                                generationSpecs[testCase.GenerationSpecId!.Value],
                                testCase.GenerationCaseIndex!.Value
                            )
                            .Select(v => new CodeTemplateInput(v.Value, v.ValueType))
                            .ToList()
                        : testCase.Inputs.Select(i => new CodeTemplateInput(i.Value, i.ValueType)).ToList();

                var templateContext = new CodeTemplateContext(
                    UserCode: userCode,
                    FunctionName: setup.FunctionName,
                    Inputs: inputs,
                    AdditionalFiles: additionalSourceFiles
                );

                return new ExecutionEngineSubmission(
                    SourceCode: templateStrategy.Render(templateContext),
                    LanguageId: judge0LanguageId,
                    Stdin: templateStrategy.BuildStdin(inputs),
                    TimeLimitMs: null,
                    MemoryLimitKb: null,
                    AdditionalFiles: templateStrategy.BuildAdditionalFiles(templateContext) ?? additionalFiles
                );
            }),
        ];
    }

    private static Dictionary<string, Guid> BuildTokenMap(
        List<TestCase> testCases,
        IReadOnlyList<ExecutionEngineResult> results
    )
    {
        return testCases
            .Zip(results, (testCase, result) => new { testCase.Id, result.Token })
            .ToDictionary(x => x.Token, x => x.Id);
    }

    private static StepHandlerResult Fail(string error) => new(Succeeded: false, Error: error);

    internal sealed record ExecutePayload(
        string LanguageName,
        TestExecutionMode Mode,
        Dictionary<string, Guid> Tokens,
        string? BatchToken = null,
        List<Guid>? BatchTestCaseIds = null
    );
}