using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Users.Entities;

public sealed class UserAvatar : AggregateRoot
{
    public UserAvatar(Guid userId, ImageUrl imageUrl)
    {
        UserId = userId;
        ImageUrl = imageUrl ?? throw new ArgumentNullException(nameof(imageUrl));
        CreatedAt = DateTime.UtcNow;
    }

    private UserAvatar() { }

    public Guid UserId { get; private set; }

    public ImageUrl ImageUrl { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
}