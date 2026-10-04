using StackDuel.Domain.User.Exceptions;

namespace StackDuel.Domain.User.ValueObjects;

public sealed record UserSub
{
    public UserSub(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidUserSubException();

        Value = value;
    }

    public static implicit operator string(UserSub sub) => sub.Value;

    public override string ToString() => Value;

    public string Value { get; }
}