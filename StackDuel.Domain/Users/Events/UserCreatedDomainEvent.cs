using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Events;

public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;