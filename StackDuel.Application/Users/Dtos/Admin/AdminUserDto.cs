using StackDuel.Application.Groups.Dtos;

namespace StackDuel.Application.Users.Dtos.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string Username,
    string? ImageUrl,
    DateTime? UsernameLastChangedAt,
    DateTime CreatedAt,
    DateTime? SetupCompletedAt,
    IReadOnlyList<GroupDto> Groups
);