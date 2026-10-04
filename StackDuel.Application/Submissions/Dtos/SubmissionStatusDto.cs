using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record SubmissionResultStatusDto(
    SubmissionResultStatus Status,
    int? Runtime,
    int? MemoryUsed,
    string? Input,
    string? ActualOutput,
    string? ExpectedOutput,
    string? StandardOutput,
    string? StandardError,
    string? CompileOutput
);

public sealed record SubmissionStatusDto(
    Guid SubmissionId,
    Guid ProblemSetupId,
    SubmissionStatus Status,
    string? CurrentStepName,
    string Code,
    IReadOnlyCollection<SubmissionResultStatusDto> Results
);