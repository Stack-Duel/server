using StackDuel.Domain.SeedWork;
using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Commands.Games.CompleteProblem;

internal sealed class CompleteProblemHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IGameProblemAdvancer gameProblemAdvancer,
    ISubmissionWriteRepository submissionRepository,
    IGameRoundSubmissionReadRepository gameRoundSubmissionReadRepository,
    IValidator<CompleteProblemCommand> validator
) : AbstractCommandHandler<CompleteProblemCommand, CompleteProblemResultDto>(validator)
{
    protected override async Task<Result<CompleteProblemResultDto>> HandleValidated(
        CompleteProblemCommand request,
        CancellationToken cancellationToken
    )
    {
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

            var (submissionFailure, _) = await ValidateSubmissionAsync(game, participant, request, cancellationToken);
            if (submissionFailure is not null)
                return submissionFailure;

            var gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
            if (gameMode is null)
                return Result<CompleteProblemResultDto>.NotFound("Game references a mode that no longer exists.");

            game.RecordProblemSolved(request.RequestedByUserId);

            Guid? nextProblemId;
            try
            {
                nextProblemId = await gameProblemAdvancer.AdvanceAsync(
                    game,
                    participant,
                    gameMode.Key,
                    onAdvance: nextProblemId =>
                        game.SolveCurrentRoundAndAdvance(
                            request.RequestedByUserId,
                            nextProblemId,
                            request.SubmissionId
                        ),
                    onFinish: null,
                    cancellationToken
                );
            }
            catch (DuplicateGameProblemException)
            {
                return Result<CompleteProblemResultDto>.Error("Failed to advance to the next problem.");
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
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

    private async Task PersistExpiryIfNeededAsync(
        Game game,
        GameProblemAttempt attempt,
        CancellationToken cancellationToken
    )
    {
        if (!attempt.GameJustExpired)
            return;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
    }

    private async Task<(Result<CompleteProblemResultDto>? Failure, Submission? Submission)> ValidateSubmissionAsync(
        Game game,
        GameParticipant participant,
        CompleteProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        Guid? currentRoundId = participant.CurrentRoundId;
        if (!currentRoundId.HasValue)
            return (
                Result<CompleteProblemResultDto>.Invalid(
                    new ValidationError(nameof(request.SubmissionId), "Participant is not on a round.")
                ),
                null
            );

        bool isLatestAttempt = await gameRoundSubmissionReadRepository.IsLatestAttemptAsync(
            participant.Id,
            currentRoundId.Value,
            request.SubmissionId,
            cancellationToken
        );

        if (!isLatestAttempt)
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