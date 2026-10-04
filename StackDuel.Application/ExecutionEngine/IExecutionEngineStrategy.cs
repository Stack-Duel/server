namespace StackDuel.Application.ExecutionEngine;

public sealed record ExecutionEngineSubmission(
    string SourceCode,
    int LanguageId,
    string? Stdin,
    int? TimeLimitMs,
    int? MemoryLimitKb,
    byte[]? AdditionalFiles = null
);

public sealed record ExecutionEngineResult(
    string Token,
    string? Stdout,
    string? Stderr,
    string? CompileOutput,
    int? RuntimeMs,
    int? MemoryUsedKb,
    ExecutionEngineResultStatus Status
);

public enum ExecutionEngineResultStatus
{
    Queued = 1,
    Processing = 2,
    Accepted = 3,
    WrongAnswer = 4,
    TimeLimitExceeded = 5,
    CompilationError = 6,
    RuntimeError = 7,
    InternalError = 8,
}

public interface IExecutionEngineStrategy
{
    Task<IReadOnlyList<ExecutionEngineResult>> SubmitBatchAsync(
        IReadOnlyList<ExecutionEngineSubmission> submissions,
        Guid? submissionId = null,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ExecutionEngineResult>> PollBatchAsync(
        IReadOnlyList<string> tokens,
        CancellationToken cancellationToken = default
    );
}