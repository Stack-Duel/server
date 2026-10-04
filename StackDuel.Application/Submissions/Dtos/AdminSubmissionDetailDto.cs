using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record AdminSubmissionDetailDto(
    Guid Id,
    SubmissionType Type,
    SubmissionStatus Status,
    string SourceCode,
    DateTime CreatedAt,
    Guid ProblemSetupId,
    Guid ProblemId,
    string ProblemTitle,
    string ProblemSlug,
    SubmissionLanguageDto Language,
    SubmissionUserDto User,
    int? MemoryUsage,
    int? ExecutionTime,
    IReadOnlyList<AdminSubmissionResultDto> Results,
    AdminSubmissionJobDto? Job
);

public sealed record AdminSubmissionResultDto(
    Guid TestCaseId,
    SubmissionResultStatus Status,
    int? Runtime,
    int? MemoryUsed,
    string? ActualOutput,
    string? StandardOutput,
    string? StandardError,
    string? CompileOutput
);

public sealed record AdminSubmissionJobDto(
    Guid Id,
    SubmissionJobStatus Status,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Guid? CurrentStepId,
    IReadOnlyList<AdminSubmissionJobStepDto> Steps
);

public enum AdminSubmissionJobStepStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
}

public sealed record AdminSubmissionJobStepDto(
    Guid StepId,
    string Name,
    ExecutionPipelineStepType StepType,
    int StepOrder,
    int MaxAttempts,
    int TimeoutSeconds,
    bool IsPolling,
    bool IsCurrent,
    AdminSubmissionJobStepStatus Status,
    int AttemptCount,
    int? TotalDurationMs,
    IReadOnlyList<AdminSubmissionJobAttemptDto> Attempts
);

public sealed record AdminSubmissionJobAttemptDto(
    int AttemptNumber,
    SubmissionJobAttemptStatus Status,
    string? RequestPayload,
    string? ResponsePayload,
    string? Error,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int? DurationMs
);