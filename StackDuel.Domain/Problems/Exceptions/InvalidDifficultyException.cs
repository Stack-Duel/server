using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class InvalidDifficultyException(string reason) : DomainException($"Difficulty is invalid: {reason}") { }