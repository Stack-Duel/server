using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.Exceptions;

namespace StackDuel.Domain.Problems.ValueObjects;

public sealed record Difficulty
{
    public Difficulty(int value)
    {
        if (value < MinValue)
            throw new InvalidDifficultyException($"Difficulty cannot be less than {MinValue}.");

        Value = value;
    }

    public override string ToString() => $"{Value} ({Tier})";

    public static readonly int BeginnerMin = 0;
    public static readonly int BeginnerMax = 200;
    public static readonly int EasyMin = 201;
    public static readonly int EasyMax = 500;
    public static readonly int IntermediateMin = 501;
    public static readonly int IntermediateMax = 1000;
    public static readonly int AdvancedMin = 1001;
    public static readonly int AdvancedMax = 2000;
    public static readonly int ExpertMin = 2001;
    public static readonly int MinValue = 0;

    public DifficultyTier Tier =>
        Value switch
        {
            <= 200 => DifficultyTier.Beginner,
            <= 500 => DifficultyTier.Easy,
            <= 1000 => DifficultyTier.Intermediate,
            <= 2000 => DifficultyTier.Advanced,
            _ => DifficultyTier.Expert,
        };

    public int Value { get; }
}