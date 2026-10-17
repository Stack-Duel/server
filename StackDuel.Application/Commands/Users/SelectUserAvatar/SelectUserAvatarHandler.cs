using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Events;

namespace StackDuel.Application.Commands.Users.SelectUserAvatar;

internal sealed class SelectUserAvatarHandler(
    IValidator<SelectUserAvatarCommand> validator,
    IUserWriteRepository userRepository,
    IUserAvatarWriteRepository userAvatarRepository,
    IDomainEventDispatcher domainEventDispatcher
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

        await domainEventDispatcher.DispatchAsync(
            [new UserAccessContextChangedDomainEvent(user.Sub)],
            cancellationToken
        );

        return Result.Success();
    }
}