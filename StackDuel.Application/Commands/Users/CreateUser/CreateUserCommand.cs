namespace StackDuel.Application.Commands.Users.CreateUser;

public sealed record CreateUserCommand(string Username, string Sub, string? ImageUrl = null, string? Tenant = null)
    : ICommand<Guid>;