namespace StackDuel.Domain.FeatureFlags.ValueObjects;

public readonly record struct RolloutPercentage
{
    public static readonly RolloutPercentage Zero = new(0);

    public RolloutPercentage(int value)
    {
        if (value is < 0 or > 100)
            throw new ArgumentException("RolloutPercentage must be between 0 and 100.", nameof(value));

        Value = value;
    }

    public int Value { get; }
}