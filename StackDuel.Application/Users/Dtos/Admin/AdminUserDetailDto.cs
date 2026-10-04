using StackDuel.Application.Groups.Dtos;

namespace StackDuel.Application.Users.Dtos.Admin;

public sealed record AdminUserDetailDto(
    Guid Id,
    string Username,
    string? ImageUrl,
    string? Bio,
    bool IsPrivate,
    DateTime? UsernameLastChangedAt,
    DateTime CreatedAt,
    DateTime? SetupCompletedAt,
    IReadOnlyList<GroupDto> Groups
);