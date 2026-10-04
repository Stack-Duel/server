using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Exceptions;

public sealed class LanguageVersionNotFoundException(Guid versionId)
    : DomainException($"Language version with ID '{versionId}' was not found.")
{ }