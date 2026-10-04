using StackDuel.Domain.Languages.Exceptions;

namespace StackDuel.Domain.Languages.ValueObjects;

public sealed record LanguageVersion
{
    public LanguageVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidLanguageVersionException("Version cannot be empty.");

        if (value.Length > MaxLength)
            throw new InvalidLanguageVersionException($"Version cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(LanguageVersion version) => version.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 50;
    public string Value { get; }
}