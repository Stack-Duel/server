namespace StackDuel.Api.Requests.Campaign;

public sealed record SetUnitProblemsRequest(IReadOnlyList<Guid> ProblemIds);