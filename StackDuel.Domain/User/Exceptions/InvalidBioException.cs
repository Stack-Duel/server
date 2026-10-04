using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.User.Exceptions;

public sealed class InvalidBioException(string reason) : DomainException($"Bio is invalid: {reason}");