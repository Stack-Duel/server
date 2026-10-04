using StackDuel.Domain.SeedWork;
using StackDuel.Domain.User.Exceptions;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Domain.User.Entities;

public sealed class User : AggregateRoot
{
    public User(Username username, UserSub sub, string? imageUrl = null, string? tenant = null)
    {
        SetUsername(username);
        SetSub(sub);
        Tenant = tenant;

        if (imageUrl is not null)
            SetImageUrl(new ImageUrl(imageUrl));
    }

    public void ChangeUsername(Username username)
    {
        if (
            UsernameLastChangedAt.HasValue
            && DateTime.UtcNow - UsernameLastChangedAt.Value < TimeSpan.FromDays(MaxDaysUntilUsernameChange)
        )
            throw new UsernameCooldownException(UsernameLastChangedAt.Value);

        SetUsername(username);
        UsernameLastChangedAt = DateTime.UtcNow;
    }

    public void UpdateBio(Bio? bio) => Bio = bio;

    public void UpdateImageUrl(ImageUrl? imageUrl) => SetImageUrl(imageUrl);

    public void CompleteSetup() => SetupCompletedAt ??= DateTime.UtcNow;

    public void SetPrivate(bool isPrivate) => IsPrivate = isPrivate;

    private void SetUsername(Username username) =>
        Username = username ?? throw new InvalidUsernameException("Username is required.");

    private void SetSub(UserSub sub) => Sub = sub ?? throw new InvalidUserSubException();

    private void SetImageUrl(ImageUrl? imageUrl) => ImageUrl = imageUrl;

    public string? Tenant { get; private set; }

    public Bio? Bio { get; private set; }

    public ImageUrl? ImageUrl { get; private set; }

    public UserSub Sub { get; private set; } = null!;

    public Username Username { get; private set; } = null!;

    public DateTime? UsernameLastChangedAt { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? SetupCompletedAt { get; private set; }

    public bool IsPrivate { get; private set; }

    public static readonly int MaxDaysUntilUsernameChange = 30;
}