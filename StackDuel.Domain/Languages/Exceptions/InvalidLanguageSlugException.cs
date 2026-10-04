using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Exceptions;

public sealed class InvalidLanguageSlugException(string reason)
    : DomainException($"Language slug is invalid: {reason}")
{ }