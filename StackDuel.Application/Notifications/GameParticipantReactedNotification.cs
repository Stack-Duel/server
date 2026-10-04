namespace StackDuel.Application.Notifications;

/// <summary>
/// Shape pushed to clients over the game hub whenever a participant sends a quick emoji reaction
/// in a Running game. Same "carries its own data, no REST fallback" treatment as
/// GameParticipantAttemptedNotification — there's nothing persisted behind this to poll instead.
/// </summary>
public sealed record GameParticipantReactedNotification(Guid GameId, Guid UserId, string Emoji, DateTime SentAt);