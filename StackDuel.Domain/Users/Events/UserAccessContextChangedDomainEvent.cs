using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Users.Events;

public sealed record UserAccessContextChangedDomainEvent(string Sub) : IDomainEvent;