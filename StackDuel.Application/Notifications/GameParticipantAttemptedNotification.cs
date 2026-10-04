using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Notifications;

/// <summary>
/// Shape pushed to clients over the game hub whenever a participant's submission in a Running
/// game reaches a terminal state. Unlike GameProgressUpdatedNotification, this carries its own
/// data (status) rather than being a bare re-fetch signal — the live activity feed is built
/// entirely from these pushes and has no REST endpoint to fall back on.
/// </summary>
public sealed record GameParticipantAttemptedNotification(
    Guid GameId,
    Guid UserId,
    SubmissionStatus Status,
    DateTime AttemptedAt
);