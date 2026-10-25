namespace StackDuel.Application.Problems.Dtos;

public sealed record CreateDraftProblemInput(
    string Title,
    string Question,
    IEnumerable<string> Tags,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    Guid TrackId
);
