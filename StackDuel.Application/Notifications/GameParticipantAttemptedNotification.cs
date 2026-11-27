using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Notifications;

public sealed record GameParticipantAttemptedNotification(
    Guid GameId,
    Guid UserId,
    SubmissionStatus Status,
    DateTime AttemptedAt
);