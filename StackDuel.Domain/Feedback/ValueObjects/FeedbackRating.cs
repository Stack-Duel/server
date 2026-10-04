using StackDuel.Domain.Feedback.Exceptions;

namespace StackDuel.Domain.Feedback.ValueObjects;

public sealed record FeedbackRating
{
    public FeedbackRating(int value)
    {
        if (value < MinValue || value > MaxValue)
            throw new InvalidFeedbackRatingException($"Rating must be between {MinValue} and {MaxValue}.");

        Value = value;
    }

    public static implicit operator int(FeedbackRating rating) => rating.Value;

    public override string ToString() => Value.ToString();

    public static readonly int MinValue = 1;
    public static readonly int MaxValue = 5;

    public int Value { get; }
}