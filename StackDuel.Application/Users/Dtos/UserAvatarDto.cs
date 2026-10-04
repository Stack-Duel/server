namespace StackDuel.Application.Users.Dtos;

public sealed record UserAvatarDto(Guid Id, string Url, DateTime CreatedAt, bool IsCurrent);