using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidQuestionException(string reason) : DomainException($"Question is invalid: {reason}") { }