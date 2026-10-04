using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Api.Requests.Problem;

public sealed record UpdateProblemRequest(
    string Title,
    string Question,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyCollection<string> Tags,
    ProblemStatus? Status
);