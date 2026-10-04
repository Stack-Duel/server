using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Submissions.Exceptions;

public sealed class InvalidSourceCodeException : DomainException
{
    public InvalidSourceCodeException(string reason)
        : base($"Source code is invalid: {reason}") { }
}