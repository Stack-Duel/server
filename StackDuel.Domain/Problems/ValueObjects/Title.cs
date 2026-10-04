using StackDuel.Domain.Problems.Exceptions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record Title
{
    public Title(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidTitleException("Title cannot be empty.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidTitleException($"Title must be between {MinLength} and {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(Title title) => title.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 200;
    public static readonly int MinLength = 3;
    public string Value { get; }
}