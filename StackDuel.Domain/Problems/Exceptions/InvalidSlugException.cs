using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidSlugException : DomainException
{
    public InvalidSlugException(string reason)
        : base($"Slug is invalid: {reason}") { }
}