using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Users.SelectUserAvatar;

internal sealed class SelectUserAvatarHandler(
    IValidator<SelectUserAvatarCommand> validator,
    IUserWriteRepository userRepository,
    IUserAvatarWriteRepository userAvatarRepository
) : AbstractCommandHandler<SelectUserAvatarCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SelectUserAvatarCommand request,
        CancellationToken cancellationToken
    )
    {
        User? user = await userRepository.FindByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound();

        UserAvatar? avatar = await userAvatarRepository.FindByIdForUserAsync(
            request.AvatarId,
            request.UserId,
            cancellationToken
        );

        if (avatar is null)
            return Result.NotFound();

        user.UpdateImageUrl(avatar.ImageUrl);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}