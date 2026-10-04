using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.User.Exceptions;

public sealed class InvalidUserSubException() : DomainException("User sub is required.");