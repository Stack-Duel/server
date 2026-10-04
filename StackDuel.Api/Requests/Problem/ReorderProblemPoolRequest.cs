namespace StackDuel.Api.Requests.Problem;

public sealed record ReorderProblemPoolRequest(IReadOnlyList<Guid> ProblemIds);