using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Tracks.Entities;

public sealed class Track : AggregateRoot
{
    public Track(string key, string name)
    {
        Key = !string.IsNullOrWhiteSpace(key)
            ? key.Trim().ToLowerInvariant()
            : throw new ArgumentException("Track key must not be empty.", nameof(key));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Track name must not be empty.", nameof(name));

        IsActive = true;
        AllowsLanguageSelection = true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void EnableLanguageSelection() => AllowsLanguageSelection = true;

    public void DisableLanguageSelection() => AllowsLanguageSelection = false;

    private Track() { }

    public string Key { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool AllowsLanguageSelection { get; private set; }
}