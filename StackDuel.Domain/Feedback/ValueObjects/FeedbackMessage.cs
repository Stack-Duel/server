using StackDuel.Domain.Feedback.Exceptions;

namespace StackDuel.Domain.Feedback.ValueObjects;

public sealed record FeedbackMessage
{
    public FeedbackMessage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidFeedbackMessageException("Message cannot be empty.");

        if (value.Length > MaxLength)
            throw new InvalidFeedbackMessageException($"Message cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public static implicit operator string(FeedbackMessage message) => message.Value;

    public override string ToString() => Value;

    public static readonly int MaxLength = 2000;

    public string Value { get; }
}