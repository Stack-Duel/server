namespace StackDuel.Domain.Authorization.Security.ValueObjects;

public readonly record struct Reason
{
    public Reason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Reason required.", nameof(value));
        Value = value;
    }

    public string Value { get; }
}