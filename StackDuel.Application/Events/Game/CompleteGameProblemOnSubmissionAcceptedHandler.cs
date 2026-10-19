using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;

namespace StackDuel.Application.Events.Games;

/// <summary>
/// Scores and advances a game participant as soon as the submission they sent is graded
/// accepted. Submitting is a single request: the player posts their code and the server finishes
/// the round for them once grading lands, so nothing is lost if they close the tab while waiting.
/// Rejected submissions leave the participant on the same problem to try again.
/// </summary>
internal sealed partial class CompleteGameProblemOnSubmissionAcceptedHandler(
    IGameReadRepository gameReadRepository,
    IGameProblemCompleter gameProblemCompleter,
    ILogger<CompleteGameProblemOnSubmissionAcceptedHandler> logger
) : INotificationHandler<DomainEventNotification<SubmissionCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SubmissionCompletedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        SubmissionCompletedDomainEvent e = notification.DomainEvent;

        if (!e.GameId.HasValue || e.Status != SubmissionStatus.Accepted)
            return;

        Guid gameId = e.GameId.Value;

        Game? game = await gameReadRepository.FindGameByIdAsync(gameId, cancellationToken);
        if (game is null)
            return;

        Guid? problemId = game.CurrentProblemIdFor(e.UserId);
        if (!problemId.HasValue)
            return;

        Result<GameProblemCompletionDto> result = await gameProblemCompleter.CompleteAsync(
            gameId,
            problemId.Value,
            e.SubmissionId,
            e.UserId,
            cancellationToken
        );

        if (!result.IsSuccess)
            LogCompletionRejected(gameId, e.SubmissionId, result.Status, string.Join("; ", result.Errors));
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Accepted submission {SubmissionId} did not complete the current round of game {GameId} ({Status}): {Errors}. The participant stays on that problem."
    )]
    private partial void LogCompletionRejected(Guid gameId, Guid submissionId, ResultStatus status, string errors);
}