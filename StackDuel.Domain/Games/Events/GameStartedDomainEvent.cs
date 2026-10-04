using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Events;

public sealed record GameStartedDomainEvent(Guid GameId, DateTime StartedAt, int TimeLimitInSeconds) : IDomainEvent;