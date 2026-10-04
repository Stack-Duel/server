using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameModeTimeOption : Entity
{
    internal GameModeTimeOption(int durationSeconds, bool isDefault)
    {
        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be greater than zero.", nameof(durationSeconds));

        DurationSeconds = durationSeconds;
        IsDefault = isDefault;
    }

    internal void MarkAsDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    private GameModeTimeOption() { }

    public int DurationSeconds { get; private set; }

    public bool IsDefault { get; private set; }
}