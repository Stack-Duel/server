namespace StackDuel.Api.Requests.User;

public sealed record UpsertUserRequest(
    string? Username,
    string? Picture,
    string? Bio,
    IReadOnlyList<Guid>? LanguageIds = null,
    string? TenantId = null
);