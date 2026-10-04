namespace StackDuel.Api.Requests.User;

public sealed record GetAdminUsersPageableRequest(int Page, int Size, DateTime Timestamp, string? Search);