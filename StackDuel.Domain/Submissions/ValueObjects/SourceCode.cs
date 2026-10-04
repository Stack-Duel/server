using StackDuel.Domain.Submissions.Exceptions;

namespace StackDuel.Domain.Submissions.ValueObjects;

public sealed record SourceCode
{
    public SourceCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidSourceCodeException("Source code cannot be empty.");

        if (value.Length > MaxLength)
            throw new InvalidSourceCodeException($"Source code cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(SourceCode code) => code.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 65536;
    public string Value { get; }
}