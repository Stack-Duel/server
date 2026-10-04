namespace StackDuel.Application.Users.Dtos;

public sealed record UserDto(Guid Id, string Username, string Sub, string? ImageUrl, string? Bio, string? Tenant);