using StackDuel.Domain.Languages.Exceptions;

namespace StackDuel.Domain.Languages.ValueObjects;

public sealed record Judge0Id
{
    public Judge0Id(int value)
    {
        if (value <= 0)
            throw new InvalidJudge0IdException($"Judge0 id must be a positive integer, got {value}.");

        Value = value;
    }

    public override string ToString() => Value.ToString();

    public int Value { get; }
}