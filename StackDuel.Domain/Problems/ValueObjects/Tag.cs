using StackDuel.Domain.Problems.Exceptions;
using System.Text.RegularExpressions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record Tag
{
    private static readonly Regex ValidTagPattern = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1)
    );

    public Tag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidTagException("Tag cannot be empty.");

        if (value.Contains(' '))
            throw new InvalidTagException("Tag cannot contain spaces.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidTagException($"Tag must be between {MinLength} and {MaxLength} characters.");

        if (!ValidTagPattern.IsMatch(value))
            throw new InvalidTagException(
                "Tag must be lowercase, contain only letters, numbers, and hyphens, and cannot start or end with a hyphen."
            );

        Value = value;
    }

    public static implicit operator string(Tag tag) => tag.Value;

    public override string ToString() => Value;

    public static readonly int MinLength = 1;
    public static readonly int MaxLength = 50;
    public string Value { get; }
}