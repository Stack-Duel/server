namespace StackDuel.Application.Users.Dtos;

public sealed record UserAccessContextDto(UserDto User, IReadOnlyList<string> Permissions, IReadOnlyList<string> Roles);