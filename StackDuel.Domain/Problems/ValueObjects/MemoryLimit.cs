using StackDuel.Domain.Problems.Exceptions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record MemoryLimit
{
    public MemoryLimit(int megabytes)
    {
        if (megabytes < MinMegabytes || megabytes > MaxMegabytes)
            throw new InvalidMemoryLimitException(
                $"Memory limit must be between {MinMegabytes}MB and {MaxMegabytes}MB."
            );

        Megabytes = megabytes;
    }

    public override string ToString() => $"{Megabytes}MB";

    public static readonly int MaxMegabytes = 512;
    public static readonly int MinMegabytes = 16;
    public int Megabytes { get; }
}