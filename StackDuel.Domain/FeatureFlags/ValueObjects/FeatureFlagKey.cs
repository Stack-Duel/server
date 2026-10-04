using System.Text.RegularExpressions;

namespace StackDuel.Domain.FeatureFlags.ValueObjects;

public readonly partial record struct FeatureFlagKey
{
    public static readonly int MaxLength = 100;

    public FeatureFlagKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FeatureFlagKey required.", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"FeatureFlagKey must be at most {MaxLength} characters.", nameof(value));

        if (!KebabCaseRegex().IsMatch(value))
            throw new ArgumentException(
                "FeatureFlagKey must be lowercase kebab-case, e.g. 'leaderboards'.",
                nameof(value)
            );

        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCaseRegex();
}