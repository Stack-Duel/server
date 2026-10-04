using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.User.Exceptions;

public sealed class InvalidImageUrlException(string reason) : DomainException($"Image URL is invalid: {reason}");