using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record StudioProblemDetailsDto(
    Guid ProblemId,
    string Slug,
    string Title,
    int DifficultyValue,
    ProblemStatus Status,
    int TimeLimitMs,
    int MemoryLimitMb,
    int SetupCount,
    DateTime CreatedAt,
    string Question,
    int ReactionCount = 0,
    int AttemptCount = 0,
    double SolveRatePercent = 0,
    DifficultyTier DifficultyTier = DifficultyTier.Beginner,
    StudioProblemPublishReadinessDto? PublishReadiness = null,
    Guid? TrackId = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<GenerationParameterSpec>? GenerationParameters = null,
    string? GenerationOutputValueType = null,
    int? GenerationTargetCaseCount = null,
    int? GenerationSeed = null,
    bool HasUnpublishedChanges = false,
    bool IsRevalidating = false,
    bool HasPublishedVersion = false
);