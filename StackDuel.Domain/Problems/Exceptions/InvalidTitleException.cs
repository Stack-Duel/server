using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidTitleException(string reason) : DomainException($"Title is invalid: {reason}") { }