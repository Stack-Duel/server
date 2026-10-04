using StackDuel.Domain.User.Exceptions;

namespace StackDuel.Domain.User.ValueObjects;

public sealed record Bio
{
    public Bio(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidBioException("Bio cannot be empty.");

        if (value.Length > MaxLength)
            throw new InvalidBioException($"Bio cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(Bio bio) => bio.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 500;

    public string Value { get; }
}