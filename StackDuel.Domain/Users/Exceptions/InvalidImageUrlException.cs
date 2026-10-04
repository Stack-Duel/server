using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Exceptions;

public sealed class InvalidImageUrlException(string reason) : DomainException($"Image URL is invalid: {reason}") { }