namespace StackDuel.Api.Requests.Problem;

public sealed record GetProblemsPageableRequest(int Page, int Size, DateTime Timestamp, string? Search);