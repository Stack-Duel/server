using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Users.SelectUserAvatar;

public sealed record SelectUserAvatarCommand(Guid UserId, Guid AvatarId) : ICommand;