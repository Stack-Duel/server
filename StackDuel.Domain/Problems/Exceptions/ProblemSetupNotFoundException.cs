using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Problems.Exceptions;

public sealed class ProblemSetupNotFoundException(Guid setupId)
    : DomainException($"Problem setup '{setupId}' was not found.")
{ }