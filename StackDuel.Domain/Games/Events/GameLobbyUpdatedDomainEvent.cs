using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Events;

public sealed record GameLobbyUpdatedDomainEvent(Guid GameId) : IDomainEvent;