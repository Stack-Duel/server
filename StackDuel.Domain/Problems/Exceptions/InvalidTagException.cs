using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidTagException(string reason) : DomainException($"Tag is invalid: {reason}") { }