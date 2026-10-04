namespace StackDuel.Application.Commands.Users.UpsertUser;

internal sealed record UpsertUserCommand(
    string Sub,
    string? Username,
    string? ImageUrl,
    string? Bio,
    IReadOnlyList<Guid>? LanguageIds = null,
    string? Tenant = null
) : ICommand;