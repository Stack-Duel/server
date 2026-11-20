using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Application.ProblemValidation.Dtos;

public sealed record ProblemValidationStepItemDto(string Label, ProblemValidationStepStatus Status, string? Detail);

public sealed record ProblemValidationStepDto(
    ProblemValidationStepKind Kind,
    ProblemValidationStepStatus Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Detail,
    IReadOnlyList<ProblemValidationStepItemDto> Items
);

public sealed record ProblemValidationAttemptSummaryDto(
    int AttemptCount,
    DateTime? LastSubmittedAt,
    DateTime? LastCompletedAt,
    ProblemValidationJobStatus? LastStatus,
    string? LastFailureReason,
    IReadOnlyList<ProblemValidationStepDto>? Steps = null,
    int ProcessingAttempts = 0,
    string? LastError = null
)
{
    public static readonly ProblemValidationAttemptSummaryDto NeverSubmitted = new(0, null, null, null, null);
}
