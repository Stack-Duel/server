using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Events;

public sealed record GameCreatedDomainEvent(Guid GameId) : IDomainEvent;