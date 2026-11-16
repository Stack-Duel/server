using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record StudioProblemPublishReadinessDto(
    bool CanSubmit,
    bool IsReady,
    int RequirementsMet,
    int RequirementCount,
    int SetupCount,
    IReadOnlyList<string> LanguagesMissingReferenceSolution,
    int SampleTestCaseCount,
    bool HasGenerationParameters,
    int AttemptCount,
    DateTime? LastSubmittedAt,
    DateTime? LastCompletedAt,
    ProblemValidationJobStatus? LastAttemptStatus,
    string? LastFailureReason
);
