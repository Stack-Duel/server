namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemPublishReadinessProjection(
    int SetupCount,
    IReadOnlyList<Guid> LanguageVersionIdsMissingReferenceSolution,
    int SampleTestCaseCount,
    bool HasGenerationParameters
);
