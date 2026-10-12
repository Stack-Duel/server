using Ardalis.Result;
using FluentValidation;
using MediatR;
using StackDuel.Application.Commands.Submissions.CreateSubmission;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Commands.Games.SubmitGameProblem;

internal sealed class SubmitGameProblemHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    ISubmissionReadRepository submissionReadRepository,
    IMediator mediator,
    IValidator<SubmitGameProblemCommand> validator
) : AbstractCommandHandler<SubmitGameProblemCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        SubmitGameProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        var game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
        if (game is null)
            return Result<Guid>.NotFound($"Game '{request.GameId}' was not found.");

        var participant = game.Participants.FirstOrDefault(p => p.UserId == request.RequestedByUserId);
        if (participant is null)
            return Result<Guid>.Forbidden();

        if (game.CompleteIfExpired(DateTime.UtcNow))
        {
            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
        }

        if (game.Status != GameStatus.Running)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(game.Status), "Only running games can accept submissions.")
            );

        if (participant.HasStoppedPlaying)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(participant.HasStoppedPlaying),
                    participant.HasForfeited
                        ? "You have forfeited this game."
                        : "You have already completed all available problems."
                )
            );

        if (participant.ProblemSession is null)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(participant.ProblemSession), "Problem session not initialized.")
            );

        if (participant.ProblemSession.CurrentProblemId != request.ProblemId)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(request.ProblemId),
                    "The specified problem is not the participant's current problem."
                )
            );

        Result<Guid>? rejection = await RejectIfProblemNotOpenForSubmissionAsync(request, cancellationToken);
        if (rejection is not null)
            return rejection;

        var submissionResult = await mediator.Send(
            new CreateSubmissionCommand(
                request.ProblemSetupId,
                SubmissionType.Submit,
                request.Code,
                request.RequestedByUserId,
                null,
                GameId: request.GameId
            ),
            cancellationToken
        );

        if (!submissionResult.IsSuccess)
            return Result<Guid>.Error("Failed to create submission.");

        return Result<Guid>.Success(submissionResult.Value);
    }

    private async Task<Result<Guid>?> RejectIfProblemNotOpenForSubmissionAsync(
        SubmitGameProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        GameProblemSubmissionDto? latest = await submissionReadRepository.FindLatestGameProblemSubmissionAsync(
            request.GameId,
            request.RequestedByUserId,
            request.ProblemId,
            cancellationToken
        );

        if (latest is null)
            return null;

        if (latest.Status == SubmissionStatus.Accepted)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(request.ProblemId), "You have already solved this problem.")
            );

        if (IsStillBeingGraded(latest))
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(request.ProblemId),
                    "Your previous submission for this problem is still being graded."
                )
            );

        return null;
    }

    /// <summary>
    /// A non-terminal submission only blocks further attempts while it is young enough that grading
    /// could still plausibly finish. Past <see cref="SubmissionGradingWindow.StaleAfter"/> the
    /// cleanup sweep owns it, and continuing to block would strand the player on a problem they can
    /// neither finish nor retry.
    /// </summary>
    private static bool IsStillBeingGraded(GameProblemSubmissionDto submission) =>
        submission.Status is SubmissionStatus.Queued or SubmissionStatus.Running
        && submission.CreatedAt > DateTime.UtcNow - SubmissionGradingWindow.StaleAfter;
}