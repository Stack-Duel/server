using StackDuel.Domain.Users.Entities;

namespace StackDuel.Application.Services.Users;

public static class AvatarRetentionPolicy
{
    public const int AvatarsToKeep = 3;

    public static IReadOnlyList<UserAvatar> SelectStale(
        IReadOnlyList<UserAvatar> avatarsNewestFirst,
        string? currentImageUrl
    )
    {
        var stale = new List<UserAvatar>();
        int keptOthers = 0;

        foreach (UserAvatar avatar in avatarsNewestFirst)
        {
            if (avatar.ImageUrl.Value == currentImageUrl)
                continue;

            if (keptOthers < AvatarsToKeep - 1)
            {
                keptOthers++;
                continue;
            }

            stale.Add(avatar);
        }

        return stale;
    }
}