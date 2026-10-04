namespace StackDuel.Domain.Authorization.Rbac.ValueObjects;

public readonly record struct PermissionCode
{
    public PermissionCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PermissionCode required.", nameof(value));
        Value = value;
    }

    public string Value { get; }
}