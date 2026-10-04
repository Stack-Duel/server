namespace StackDuel.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Sub,
    string Username,
    string? ImageUrl,
    string? Bio,
    bool IsPrivate,
    DateTime? UsernameLastChangedAt,
    DateTime CreatedAt,
    DateTime? SetupCompletedAt,
    IReadOnlyList<Guid> LanguagePreferenceIds
);