using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Games;
using StackDuel.Application.Notifications;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;

namespace StackDuel.Application.Events.Submissions;

/// <summary>
/// Reacts to a submission finishing grading: applies game progress for a submission made inside a
/// game, then pushes the submission and game notifications.
/// </summary>
internal sealed partial class SubmissionCompletedDomainEventHandler(
    ISubmissionNotificationService submissionNotificationService,
    IGameNotificationService gameNotificationService,
    IGameSubmissionProgressApplier gameSubmissionProgressApplier,
    ILogger<SubmissionCompletedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<SubmissionCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SubmissionCompletedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;

        if (e.GameId.HasValue && e.Status == SubmissionStatus.Accepted)
            await ApplyGameProgressAsync(e.SubmissionId, cancellationToken);

        await NotifySubmissionCompletedAsync(e.UserId, e.SubmissionId, cancellationToken);

        if (e.GameId.HasValue)
            await NotifyGameAttemptAsync(e.GameId.Value, e.UserId, e.Status, e.SubmissionId, cancellationToken);
    }

    private async Task ApplyGameProgressAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        try
        {
            var progress = await gameSubmissionProgressApplier.ApplyAsync(submissionId, cancellationToken);

            if (!progress.IsSuccess)
                LogApplyProgressUnsuccessful(submissionId, progress.Status);
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
        {
            LogApplyProgressFailed(ex, submissionId);
        }
    }

    private async Task NotifySubmissionCompletedAsync(
        Guid userId,
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await submissionNotificationService.NotifySubmissionCompletedAsync(userId, submissionId, cancellationToken);
        }
        catch (Exception ex)
        {
            LogNotifyFailed(ex, submissionId);
        }
    }

    private async Task NotifyGameAttemptAsync(
        Guid gameId,
        Guid userId,
        SubmissionStatus status,
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await gameNotificationService.NotifyGameParticipantAttemptedAsync(
                gameId,
                userId,
                status,
                DateTime.UtcNow,
                cancellationToken
            );
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
        {
            LogNotifyGameAttemptFailed(ex, gameId, submissionId);
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

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to apply game progress from submission {SubmissionId}; the next game read will heal it."
    )]
    private partial void LogApplyProgressFailed(Exception ex, Guid submissionId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Applying game progress from submission {SubmissionId} returned {ResultStatus}; the next game read will heal it."
    )]
    private partial void LogApplyProgressUnsuccessful(Guid submissionId, ResultStatus resultStatus);
}