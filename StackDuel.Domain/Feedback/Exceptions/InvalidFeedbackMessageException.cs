using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Feedback.Exceptions;

public sealed class InvalidFeedbackMessageException : DomainException
{
    public InvalidFeedbackMessageException(string reason)
        : base($"Feedback message is invalid: {reason}") { }
}