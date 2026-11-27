using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Notifications;

public sealed record GameCompletedNotification(Guid GameId, GameStatus Status, DateTime EndedAt);