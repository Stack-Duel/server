using StackDuel.Domain.ExecutionAssets.Exceptions;

namespace StackDuel.Domain.ExecutionAssets.ValueObjects;

public sealed record AdditionalFileBundleName
{
    public AdditionalFileBundleName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidAdditionalFileBundleNameException("Name cannot be empty.");

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new InvalidAdditionalFileBundleNameException(
                $"Name must be between {MinLength} and {MaxLength} characters."
            );

        Value = value;
    }

    public static implicit operator string(AdditionalFileBundleName name) => name.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 100;
    public static readonly int MinLength = 1;
    public string Value { get; }
}