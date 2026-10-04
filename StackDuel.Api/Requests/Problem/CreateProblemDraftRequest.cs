using System.Text.Json.Serialization;

namespace StackDuel.Api.Requests.Problem;

public sealed record CreateProblemDraftRequest(
    string Title,
    string Question,
    [property: JsonRequired] int Difficulty,
    [property: JsonRequired] int TimeLimitMs,
    [property: JsonRequired] int MemoryLimitMb,
    IReadOnlyCollection<string> Tags,
    [property: JsonRequired] Guid TrackId
);