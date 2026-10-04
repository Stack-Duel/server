using StackDuel.Application.Images;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Users.UploadUserAvatar;

internal sealed class UploadUserAvatarHandler(
    IValidator<UploadUserAvatarCommand> validator,
    IUserWriteRepository userRepository,
    IUserAvatarWriteRepository userAvatarRepository,
    IAvatarBlobStorage avatarBlobStorage
) : AbstractCommandHandler<UploadUserAvatarCommand, string>(validator)
{
    private const int MaxAvatarsToKeep = 3;

    protected override async Task<Result<string>> HandleValidated(
        UploadUserAvatarCommand request,
        CancellationToken cancellationToken
    )
    {
        User? user = await userRepository.FindByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound();

        ImageFormat format = ImageSignature.Detect(request.Content)!;

        var avatarId = Guid.NewGuid();
        string blobUrl = await avatarBlobStorage.UploadAsync(
            request.UserId,
            avatarId,
            request.Content,
            format.ContentType,
            cancellationToken
        );

        var avatar = new UserAvatar(request.UserId, new ImageUrl(blobUrl));
        await userAvatarRepository.AddAsync(avatar, cancellationToken);

        user.UpdateImageUrl(new ImageUrl(blobUrl));
        await userRepository.UpdateAsync(user, cancellationToken);

        await PruneOldAvatarsAsync(request.UserId, cancellationToken);

        return Result.Success(blobUrl);
    }

    private async Task PruneOldAvatarsAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<UserAvatar> avatars = await userAvatarRepository.GetAllByUserIdNewestFirstAsync(
            userId,
            cancellationToken
        );

        foreach (UserAvatar stale in avatars.Skip(MaxAvatarsToKeep))
        {
            await avatarBlobStorage.DeleteAsync(stale.ImageUrl.Value, cancellationToken);
            await userAvatarRepository.DeleteAsync(stale, cancellationToken);
        }
    }
}