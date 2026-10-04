using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Notifications;

/// <summary>
/// Shape pushed to clients over the game hub when a game reaches a terminal state. Kept in the
/// Application layer so the payload shape has one definition regardless of transport.
/// </summary>
public sealed record GameCompletedNotification(Guid GameId, GameStatus Status, DateTime EndedAt);