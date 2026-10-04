using StackDuel.Domain.User.Exceptions;

namespace StackDuel.Domain.User.ValueObjects;

public sealed record Username
{
    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidUsernameException("Username cannot be null or whitespace.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidUsernameException($"Username must be between {MinLength} and {MaxLength} characters.");

        if (!value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            throw new InvalidUsernameException("Username can only contain letters, digits, underscores, or hyphens.");

        Value = value;
    }

    public static implicit operator string(Username username) => username.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 20;

    public static readonly int MinLength = 1;

    public string Value { get; }
}