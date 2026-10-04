namespace StackDuel.Api.Requests.Problem;

public sealed record GetAdminProblemsPageableRequest(int Page, int Size, DateTime Timestamp, string? Search);