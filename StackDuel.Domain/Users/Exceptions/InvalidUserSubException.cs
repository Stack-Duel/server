using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Exceptions;

public sealed class InvalidUserSubException : DomainException
{
    public InvalidUserSubException()
        : base("User sub is required.") { }
}