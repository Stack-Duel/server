namespace StackDuel.Api.Requests.Problem;

public sealed record CreateDraftProblemRequest(
    string Title,
    string Question,
    int Difficulty,
    int MemoryLimitMb,
    IEnumerable<string> Tags,
    Guid TrackId
);
