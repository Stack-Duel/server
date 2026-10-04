namespace StackDuel.Application.ExecutionEngine;

/// <summary>Strategy used to compare a case's actual output against its expected one in a batched run.</summary>
public enum BatchAssertStrategy
{
    ExactMatch,
    FloatTolerance,
    SetEquality,
}

public sealed record BatchTestCase(
    Guid TestCaseId,
    IReadOnlyList<CodeTemplateInput> Inputs,
    string ExpectedOutput,
    BatchAssertStrategy AssertStrategy,
    decimal? Tolerance = null,
    bool CaseSensitive = true
);

public enum BatchCaseStatus
{
    Accepted,
    WrongAnswer,
    TimedOut,
    RuntimeError,
}

public sealed record BatchCaseResult(
    Guid TestCaseId,
    BatchCaseStatus Status,
    string? ActualOutput,
    string? ErrorMessage,
    int? RuntimeMs = null
);

/// <summary>
/// Renders every test case for one submission into a single source file that runs under the
/// language's native test framework (e.g. Vitest for JavaScript) and parses that framework's
/// own result report back into a per-case verdict — one Judge0 submission covers every case
/// instead of one submission each. Only used for a <see cref="ICodeTemplateStrategy"/> whose
/// language version opted into <see cref="StackDuel.Domain.Languages.Enums.TestExecutionMode.BatchedTestFramework"/>.
/// </summary>
public interface IBatchCodeTemplateStrategy
{
    string LanguageName { get; }

    string RenderBatch(
        string userCode,
        string? functionName,
        IReadOnlyList<BatchTestCase> cases,
        int perCaseTimeoutMs,
        IReadOnlyList<CodeTemplateSourceFile>? additionalFiles = null
    );

    IReadOnlyList<BatchCaseResult> ParseBatchOutput(string? stdout, IReadOnlyList<Guid> orderedTestCaseIds);
}