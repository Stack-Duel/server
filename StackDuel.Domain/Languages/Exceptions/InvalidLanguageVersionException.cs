using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Exceptions;

public sealed class InvalidLanguageVersionException(string reason)
    : DomainException($"Language version is invalid: {reason}")
{ }