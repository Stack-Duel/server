namespace StackDuel.Application.Notifications;

public sealed record GameParticipantReactedNotification(Guid GameId, Guid UserId, string Emoji, DateTime SentAt);