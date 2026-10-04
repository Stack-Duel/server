using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users.Events;
using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Users.Entities;

public sealed class User(Username username, string sub, string? tenant = null) : AggregateRoot
{
    public void ChangeUsername(Username username)
    {
        if (
            UsernameLastChangedAt.HasValue
            && DateTime.UtcNow - UsernameLastChangedAt.Value < TimeSpan.FromDays(MaxDaysUntilUsernameChange)
        )
            throw new UsernameCooldownException(UsernameLastChangedAt.Value);

        Username = username;
        UsernameLastChangedAt = DateTime.UtcNow;
    }

    public void UpdateBio(Bio? bio)
    {
        Bio = bio;
    }

    public void UpdateImageUrl(ImageUrl? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void CompleteSetup() => SetupCompletedAt ??= DateTime.UtcNow;

    public void SetPrivate(bool isPrivate) => IsPrivate = isPrivate;

    public void MarkCreated() => AddDomainEvent(new UserCreatedDomainEvent(Id));

    public string? Tenant { get; private set; } = tenant;

    public Bio? Bio { get; private set; }

    public ImageUrl? ImageUrl { get; private set; }

    public string Sub { get; private set; } =
        string.IsNullOrWhiteSpace(sub) ? throw new InvalidUserSubException() : sub;

    public Username Username { get; private set; } =
        username ?? throw new InvalidUsernameException("Username is required.");

    public DateTime? UsernameLastChangedAt { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? SetupCompletedAt { get; private set; }

    public bool IsPrivate { get; private set; }

    public static readonly int MaxDaysUntilUsernameChange = 30;
}