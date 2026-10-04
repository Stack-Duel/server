using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Users.UpdateProfilePrivacy;

internal sealed record UpdateProfilePrivacyCommand(Guid UserId, bool IsPrivate) : ICommand;