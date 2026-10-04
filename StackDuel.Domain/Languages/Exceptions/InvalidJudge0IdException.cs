using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Exceptions;

public sealed class InvalidJudge0IdException(string message) : DomainException(message) { }