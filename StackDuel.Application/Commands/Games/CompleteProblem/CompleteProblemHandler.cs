using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Games.CompleteProblem;

internal sealed class CompleteProblemHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    IGameProblemAdvancer gameProblemAdvancer,
    ISubmissionWriteRepository submissionRepository,
    IValidator<CompleteProblemCommand> validator
) : AbstractCommandHandler<CompleteProblemCommand, CompleteProblemResultDto>(validator)
{
    protected override async Task<Result<CompleteProblemResultDto>> HandleValidated(
        CompleteProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        // Retried once: GetOrGenerateProblemAsync (via gameProblemAdvancer) can race another
        // participant who's also first to reach this position (see GameProblemConfiguration's
        // (game_id, position) unique index). The loser gets GameProblemPositionConflictException
        // out of SaveChangesAsync — re-fetching from scratch picks up the winner's already-persisted
        // problem via Game.ProblemIdAtPosition instead of generating a conflicting second one.
        int saveAttempt = 0;
        while (true)
        {
            var game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
            if (game is null)
                return Result<CompleteProblemResultDto>.NotFound($"Game '{request.GameId}' was not found.");

            var attempt = game.EvaluateProblemAttempt(
                request.RequestedByUserId,
                request.ProblemId,
                ParticipantAction.CompleteProblem,
                DateTime.UtcNow
            );

            await PersistExpiryIfNeededAsync(game, attempt, cancellationToken);

            if (!attempt.IsEligible)
                return GameProblemAttemptResult.ToFailure<CompleteProblemResultDto>(
                    attempt,
                    ParticipantAction.CompleteProblem
                );

            var participant = attempt.Participant!;

            var (submissionFailure, _) = await ValidateSubmissionAsync(participant, request, cancellationToken);
            if (submissionFailure is not null)
                return submissionFailure;

            // Resolved before any state mutation: a game mode that no longer exists is a genuine
            // system error and should bail out cleanly, not after the participant's score has
            // already been bumped in memory for a solve that then can't be persisted.
            var gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
            if (gameMode is null)
                return Result<CompleteProblemResultDto>.NotFound("Game references a mode that no longer exists.");

            game.RecordProblemSolved(request.RequestedByUserId);

            Guid? nextProblemId = await gameProblemAdvancer.AdvanceAsync(
                game,
                participant,
                gameMode.Key,
                onAdvance: participant.AdvanceProblem,
                onFinish: null,
                cancellationToken
            );

            try
            {
                await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            }
            catch (GameProblemPositionConflictException) when (saveAttempt == 0)
            {
                saveAttempt++;
                continue;
            }

            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

            return Result<CompleteProblemResultDto>.Success(
                new CompleteProblemResultDto(participant.Score, nextProblemId)
            );
        }
    }

    // The expiry check inside EvaluateProblemAttempt may have just completed the game — that
    // must be persisted regardless of whether this request itself goes on to succeed.
    private async Task PersistExpiryIfNeededAsync(
        Game game,
        GameProblemAttempt attempt,
        CancellationToken cancellationToken
    )
    {
        if (!attempt.GameJustExpired)
            return;

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
    }

    // Verifies the submission was created through the game's own submit endpoint, belongs to the
    // requester, and was accepted. Returns the failing Result on the left when invalid, or the
    // validated submission on the right when it's safe to proceed.
    private async Task<(Result<CompleteProblemResultDto>? Failure, Submission? Submission)> ValidateSubmissionAsync(
        GameParticipant participant,
        CompleteProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!participant.ProblemSession!.HasActiveSubmission(request.SubmissionId))
            return (
                Result<CompleteProblemResultDto>.Invalid(
                    new ValidationError(nameof(request.SubmissionId), "Submission was not created through the game.")
                ),
                null
            );

        var submission = await submissionRepository.FindByIdAsync(request.SubmissionId, cancellationToken);
        if (submission is null || submission.UserId != request.RequestedByUserId)
            return (Result<CompleteProblemResultDto>.NotFound("Submission not found."), null);

        if (submission.Status != SubmissionStatus.Accepted)
            return (
                Result<CompleteProblemResultDto>.Invalid(
                    new ValidationError(
                        nameof(request.SubmissionId),
                        "Only an accepted submission can be used to complete a problem."
                    )
                ),
                null
            );

        return (null, submission);
    }
}