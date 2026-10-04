using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Events;

/// <summary>
/// Raised whenever a participant's progress changes in a Running game — currently, solving a
/// problem (see Game.RecordProblemSolved). Consumers (e.g. the SignalR notification handler)
/// use this to tell everyone else in the game to refresh, so opponents' scores on the Score tab
/// update promptly instead of sitting stale until something else happens to trigger a refetch.
/// </summary>
public sealed record GameProgressUpdatedDomainEvent(Guid GameId) : IDomainEvent;