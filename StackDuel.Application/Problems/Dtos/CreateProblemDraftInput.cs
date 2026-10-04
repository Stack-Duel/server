namespace StackDuel.Application.Problems.Dtos;

public sealed record CreateProblemDraftInput(
    string Title,
    string Question,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyCollection<string> Tags,
    Guid TrackId
);