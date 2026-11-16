using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Application.ProblemValidation.Dtos;

public sealed record ProblemValidationAttemptSummaryDto(
    int AttemptCount,
    DateTime? LastSubmittedAt,
    DateTime? LastCompletedAt,
    ProblemValidationJobStatus? LastStatus,
    string? LastFailureReason
)
{
    public static readonly ProblemValidationAttemptSummaryDto NeverSubmitted = new(0, null, null, null, null);
}
