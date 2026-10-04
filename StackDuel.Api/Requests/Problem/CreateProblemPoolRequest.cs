namespace StackDuel.Api.Requests.Problem;

public sealed record CreateProblemPoolRequest(string Key, string Name, string? Description);