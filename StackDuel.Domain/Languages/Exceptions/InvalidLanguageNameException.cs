using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Exceptions;

public sealed class InvalidLanguageNameException : DomainException
{
    public InvalidLanguageNameException(string reason)
        : base($"Language name is invalid: {reason}") { }
}