using System.Text.RegularExpressions;

namespace StackDuel.Domain.Achievements.ValueObjects;

public readonly partial record struct AchievementCode
{
    public static readonly int MaxLength = 100;

    public AchievementCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AchievementCode required.", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"AchievementCode must be at most {MaxLength} characters.", nameof(value));

        if (!KebabCaseRegex().IsMatch(value))
            throw new ArgumentException(
                "AchievementCode must be lowercase kebab-case, e.g. 'first-blood'.",
                nameof(value)
            );

        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCaseRegex();
}