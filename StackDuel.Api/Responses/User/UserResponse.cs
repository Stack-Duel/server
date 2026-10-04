using StackDuel.Application.Users.Dtos;

namespace StackDuel.Api.Responses.User;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string? ImageUrl,
    string? Bio,
    bool IsPrivate,
    DateTime? UsernameLastChangedAt,
    DateTime CreatedAt,
    DateTime? SetupCompletedAt,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> LanguagePreferenceIds
)
{
    public static UserResponse FromDto(UserDto dto, IReadOnlyList<string> permissions, IReadOnlyList<string> roles) =>
        new(
            dto.Id,
            dto.Username,
            dto.ImageUrl,
            dto.Bio,
            dto.IsPrivate,
            dto.UsernameLastChangedAt,
            dto.CreatedAt,
            dto.SetupCompletedAt,
            permissions,
            roles,
            dto.LanguagePreferenceIds
        );
}