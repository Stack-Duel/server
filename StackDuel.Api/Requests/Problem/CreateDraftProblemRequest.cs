namespace StackDuel.Api.Requests.Problem;

public sealed record CreateDraftProblemRequest(
    string Title,
    string Question,
    IEnumerable<string> Tags,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    Guid TrackId
);