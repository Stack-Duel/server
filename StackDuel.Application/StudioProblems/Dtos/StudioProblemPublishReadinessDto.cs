using StackDuel.Application.ProblemValidation.Dtos;
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
    string? LastFailureReason,
    IReadOnlyList<ProblemValidationStepDto>? Steps = null,
    int ProcessingAttempts = 0,
    string? LastError = null,
    bool HasUnpublishedChanges = false,
    bool IsRevalidating = false
);
