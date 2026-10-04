using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Users.UploadUserAvatar;

public sealed record UploadUserAvatarCommand(Guid UserId, byte[] Content) : ICommand<string>;