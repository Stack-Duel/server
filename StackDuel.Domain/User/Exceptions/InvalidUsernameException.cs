using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.User.Exceptions;

public sealed class InvalidUsernameException(string reason) : DomainException($"Username is invalid: {reason}");