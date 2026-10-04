using StackDuel.Domain.Languages.Exceptions;

namespace StackDuel.Domain.Languages.ValueObjects;

public sealed record LanguageName
{
    public LanguageName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidLanguageNameException("Name cannot be empty.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidLanguageNameException($"Name must be between {MinLength} and {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(LanguageName name) => name.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 100;
    public static readonly int MinLength = 1;
    public string Value { get; }
}