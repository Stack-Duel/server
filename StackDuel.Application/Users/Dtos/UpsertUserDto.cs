namespace StackDuel.Application.Users.Dtos;

public sealed record UpsertUserDto(
    string? Username,
    string? ImageUrl,
    string? Bio,
    IReadOnlyList<Guid>? LanguageIds = null,
    string? TenantId = null
);