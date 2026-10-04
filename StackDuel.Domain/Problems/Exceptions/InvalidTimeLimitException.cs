using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidTimeLimitException(string reason) : DomainException($"Time limit is invalid: {reason}") { }