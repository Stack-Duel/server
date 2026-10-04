using System.ComponentModel.DataAnnotations;

namespace StackDuel.Domain.Authorization.Rbac.ValueObjects;

public sealed record Name
{
    public Name(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name required.", nameof(value));
        if (value.Length > MaxLength)
            throw new ArgumentException($"Name must be at least {MaxLength} characters.", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public static readonly int MaxLength = 200;
}