using StackDuel.Domain.ValueTypes.Exceptions;
using System.Text.RegularExpressions;

namespace StackDuel.Domain.ValueTypes.ValueObjects;

public sealed record ValueTypeKey
{
    public const int MinLength = 2;
    public const int MaxLength = 50;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly Regex ValidKeyPattern = new(
        @"^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$",
        RegexOptions.Compiled,
        RegexTimeout
    );

    public ValueTypeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueTypeDefinitionException("Value type key must not be empty.");

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            throw new InvalidValueTypeDefinitionException(
                $"Value type key must be between {MinLength} and {MaxLength} characters."
            );

        if (!ValidKeyPattern.IsMatch(normalized))
            throw new InvalidValueTypeDefinitionException(
                "Value type key must be lowercase letters, digits and underscores, starting with a letter."
            );

        Value = normalized;
    }

    public static implicit operator string(ValueTypeKey key) => key.Value;

    public override string ToString() => Value;

    public string Value { get; }
}