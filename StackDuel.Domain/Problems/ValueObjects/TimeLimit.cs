using StackDuel.Domain.Problems.Exceptions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record TimeLimit
{
    public TimeLimit(int milliseconds)
    {
        if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds)
            throw new InvalidTimeLimitException(
                $"Time limit must be between {MinMilliseconds}ms and {MaxMilliseconds}ms."
            );

        Milliseconds = milliseconds;
    }

    public override string ToString() => $"{Milliseconds}ms";

    public static readonly int MaxMilliseconds = 10000;
    public static readonly int MinMilliseconds = 100;
    public int Milliseconds { get; }
}