using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Exceptions;

public sealed class InvalidBioException : DomainException
{
    public InvalidBioException(string reason)
        : base($"Bio is invalid: {reason}") { }
}