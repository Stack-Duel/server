namespace StackDuel.Api.Requests.Problem;

public sealed record AddProblemsToPoolRequest(
    IReadOnlyList<Guid> ProblemIds,
    bool SelectAllMatching,
    string? Search,
    IReadOnlyList<Guid> ExcludedProblemIds
);