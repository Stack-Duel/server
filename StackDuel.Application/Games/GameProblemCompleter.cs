using Ardalis.Result;
using StackDuel.Application.Events;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Games;

internal sealed class GameProblemCompleter(
    IGameReadRepository gameReadRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IGameProblemAdvancer gameProblemAdvancer,
    ISubmissionWriteRepository submissionRepository,
    IGameRoundSubmissionReadRepository gameRoundSubmissionReadRepository
) : IGameProblemCompleter
{
    public async Task<Result<GameProblemCompletionDto>> CompleteAsync(
        Guid gameId,
        Guid problemId,
        Guid submissionId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await gameReadRepository.FindGameByIdAsync(gameId, cancellationToken);
        if (game is null)
            return Result<GameProblemCompletionDto>.NotFound($"Game '{gameId}' was not found.");

        var attempt = game.EvaluateProblemAttempt(
            requestedByUserId,
            problemId,
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        await PersistExpiryIfNeededAsync(game, attempt, cancellationToken);

        if (!attempt.IsEligible)
            return GameProblemAttemptResult.ToFailure<GameProblemCompletionDto>(attempt, ParticipantAction.CompleteProblem);

        var participant = attempt.Participant!;

        Result<GameProblemCompletionDto>? submissionFailure = await ValidateSubmissionAsync(
            participant,
            submissionId,
            requestedByUserId,
            cancellationToken
        );
        if (submissionFailure is not null)
            return submissionFailure;

        var gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
        if (gameMode is null)
            return Result<GameProblemCompletionDto>.NotFound("Game references a mode that no longer exists.");

        game.RecordProblemSolved(requestedByUserId);
        game.SolveCurrentRound(requestedByUserId, submissionId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        GameProblemAdvanceResult advanceResult;
        try
        {
            advanceResult = await gameProblemAdvancer.AdvanceAsync(
                game,
                requestedByUserId,
                gameMode.Key,
                cancellationToken
            );
        }
        catch (DuplicateGameProblemException)
        {
            return Result<GameProblemCompletionDto>.Error("Failed to advance to the next problem.");
        }

        await domainEventDispatcher.DispatchAsync(advanceResult.Game.PopDomainEvents(), cancellationToken);

        return Result<GameProblemCompletionDto>.Success(
            new GameProblemCompletionDto(participant.Score, advanceResult.NextProblemId)
        );
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

    private async Task<Result<GameProblemCompletionDto>?> ValidateSubmissionAsync(
        GameParticipant participant,
        Guid submissionId,
        Guid requestedByUserId,
        CancellationToken cancellationToken
    )
    {
        Guid? currentRoundId = participant.CurrentRoundId;
        if (!currentRoundId.HasValue)
            return Result<GameProblemCompletionDto>.Invalid(
                new ValidationError(nameof(submissionId), "Participant is not on a round.")
            );

        bool isLatestAttempt = await gameRoundSubmissionReadRepository.IsLatestAttemptAsync(
            participant.Id,
            currentRoundId.Value,
            submissionId,
            cancellationToken
        );

        if (!isLatestAttempt)
            return Result<GameProblemCompletionDto>.Invalid(
                new ValidationError(nameof(submissionId), "Submission was not created through the game.")
            );

        Submission? submission = await submissionRepository.FindByIdAsync(submissionId, cancellationToken);
        if (submission is null || submission.UserId != requestedByUserId)
            return Result<GameProblemCompletionDto>.NotFound("Submission not found.");

        if (submission.Status != SubmissionStatus.Accepted)
            return Result<GameProblemCompletionDto>.Invalid(
                new ValidationError(
                    nameof(submissionId),
                    "Only an accepted submission can be used to complete a problem."
                )
            );

        return null;
    }
}