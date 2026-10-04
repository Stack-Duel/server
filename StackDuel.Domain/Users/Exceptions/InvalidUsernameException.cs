using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Exceptions;

public sealed class InvalidUsernameException(string reason) : DomainException($"Username is invalid: {reason}") { }