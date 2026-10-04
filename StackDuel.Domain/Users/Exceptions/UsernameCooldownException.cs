using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Exceptions;

public sealed class UsernameCooldownException(DateTime lastChangedAt)
    : DomainException($"Username can only be changed once every 30 days. Last changed at: {lastChangedAt}.")
{ }