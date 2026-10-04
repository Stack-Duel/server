using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Events;

/// <summary>
/// Raised whenever a game reaches a terminal Completed state — whether via the normal
/// GameTimeExpiredMessage path, the sweep-job backstop, or an early player forfeit. Consumers
/// (e.g. the SignalR notification handler) use this as the single trigger point for telling
/// clients a game is over, regardless of which path finalized it.
/// </summary>
public sealed record GameCompletedDomainEvent(Guid GameId, GameStatus Status, DateTime EndedAt) : IDomainEvent;