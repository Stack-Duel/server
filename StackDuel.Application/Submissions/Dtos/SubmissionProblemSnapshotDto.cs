namespace StackDuel.Application.Submissions.Dtos;

public sealed record SubmissionProblemSnapshotDto(
    Guid SubmissionId,
    Guid ProblemId,
    string ProblemSlug,
    int? VersionNumber,
    string Title,
    string Question,
    int DifficultyValue,
    int TimeLimitMs,
    int MemoryLimitMb,
    string InitialCode,
    string? FunctionName
)
{
    public bool IsPinnedToAVersion => VersionNumber.HasValue;
}