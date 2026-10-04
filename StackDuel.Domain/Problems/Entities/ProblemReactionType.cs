using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Entities;

public sealed class ProblemReactionType : AggregateRoot
{
    public ProblemReactionType(string key, string name, string? emoji, int sortOrder)
    {
        Key = !string.IsNullOrWhiteSpace(key)
            ? key.Trim().ToLowerInvariant()
            : throw new ArgumentException("Reaction type key must not be empty.", nameof(key));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Reaction type name must not be empty.", nameof(name));

        Emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim();
        SortOrder = sortOrder;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    private ProblemReactionType() { }

    public void Disable() => IsEnabled = false;

    public string Key { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Emoji { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; }
}