using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Feedback.Exceptions;

public sealed class InvalidFeedbackRatingException : DomainException
{
    public InvalidFeedbackRatingException(string reason)
        : base($"Feedback rating is invalid: {reason}") { }
}