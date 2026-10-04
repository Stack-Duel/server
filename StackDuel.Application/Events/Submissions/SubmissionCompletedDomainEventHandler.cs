using StackDuel.Application.Notifications;
using StackDuel.Domain.Submissions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Events.Submissions;

internal sealed partial class SubmissionCompletedDomainEventHandler(
    ISubmissionNotificationService submissionNotificationService,
    IGameNotificationService gameNotificationService,
    ILogger<SubmissionCompletedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<SubmissionCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SubmissionCompletedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;
        try
        {
            await submissionNotificationService.NotifySubmissionCompletedAsync(
                e.UserId,
                e.SubmissionId,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            LogNotifyFailed(ex, e.SubmissionId);
        }

        if (e.GameId is not { } gameId)
            return;

        try
        {
            await gameNotificationService.NotifyGameParticipantAttemptedAsync(
                gameId,
                e.UserId,
                e.Status,
                DateTime.UtcNow,
                cancellationToken
            );
        }
        catch (Exception ex)
            when (ex is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
        {
            LogNotifyGameAttemptFailed(ex, gameId, e.SubmissionId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to push submission-completed notification for submission {SubmissionId}; clients will pick up the new state on their next poll."
    )]
    private partial void LogNotifyFailed(Exception ex, Guid submissionId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to push game-participant-attempted notification for game {GameId}, submission {SubmissionId}; the live activity feed will just miss this entry."
    )]
    private partial void LogNotifyGameAttemptFailed(Exception ex, Guid gameId, Guid submissionId);
}