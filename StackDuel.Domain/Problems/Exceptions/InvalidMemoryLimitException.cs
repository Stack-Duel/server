using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidMemoryLimitException(string reason)
    : DomainException($"Memory limit is invalid: {reason}")
{ }