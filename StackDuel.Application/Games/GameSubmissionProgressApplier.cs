using Ardalis.Result;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Events;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Games;

internal sealed partial class GameSubmissionProgressApplier(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    IGameProblemAdvancer gameProblemAdvancer,
    ISubmissionReadRepository submissionReadRepository,
    ILogger<GameSubmissionProgressApplier> logger
) : IGameSubmissionProgressApplier
{
    public async Task<Result<GameSubmissionProgressResult>> ApplyAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    )
    {
        GameProblemSubmissionDto? submission = await submissionReadRepository.FindGameProblemSubmissionAsync(
            submissionId,
            cancellationToken
        );

        if (submission is null || submission.Status != SubmissionStatus.Accepted)
            return NotApplicable(score: null);

        Game? game = await gameReadRepository.FindGameByIdAsync(submission.GameId, cancellationToken);
        if (game is null)
            return GameNotFound(submission.GameId);

        int saveAttempt = 0;
        while (true)
        {
            GameParticipant? participant = game.Participants.FirstOrDefault(p => p.UserId == submission.UserId);
            if (participant is null)
            {
                LogParticipantMissing(submission.SubmissionId, submission.GameId, submission.UserId);
                return NotApplicable(score: null);
            }

            if (!IsStillOnSubmittedProblem(participant, submission))
                return NotApplicable(participant.Score);

            var attempt = game.EvaluateProblemAttempt(
                submission.UserId,
                submission.ProblemId,
                ParticipantAction.CompleteProblem,
                DateTime.UtcNow
            );

            await PersistExpiryIfNeededAsync(game, attempt, cancellationToken);

            if (!attempt.IsEligible)
                return GameProblemAttemptResult.ToFailure<GameSubmissionProgressResult>(
                    attempt,
                    ParticipantAction.CompleteProblem
                );

            GameMode? gameMode = await ResolveGameModeBeforeMutatingAsync(game, cancellationToken);
            if (gameMode is null)
                return Result<GameSubmissionProgressResult>.NotFound("Game references a mode that no longer exists.");

            Guid? nextProblemId = await ScoreAndAdvanceAsync(game, participant, gameMode.Key, cancellationToken);

            try
            {
                await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            }
            catch (GameProblemPositionConflictException) when (saveAttempt == 0)
            {
                saveAttempt++;

                game = await gameWriteRepository.ReloadAsync(submission.GameId, cancellationToken);
                if (game is null)
                    return GameNotFound(submission.GameId);

                continue;
            }

            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

            return Result<GameSubmissionProgressResult>.Success(
                new GameSubmissionProgressResult(
                    GameSubmissionProgressOutcome.Applied,
                    participant.Score,
                    nextProblemId
                )
            );
        }
    }

    public async Task<GamePendingProgressOutcome> ApplyPendingAsync(
        Game game,
        IReadOnlyList<GameProblemSubmissionDto> latestSubmissions,
        CancellationToken cancellationToken = default
    )
    {
        if (game.Status != GameStatus.Running)
            return GamePendingProgressOutcome.NoChange;

        List<GameParticipant> pending = [.. game.Participants.Where(p => HasUnscoredSolve(p, latestSubmissions))];
        if (pending.Count == 0)
            return GamePendingProgressOutcome.NoChange;

        GameMode? gameMode = await ResolveGameModeBeforeMutatingAsync(game, cancellationToken);
        if (gameMode is null)
        {
            LogGameModeMissing(game.Id, game.GameModeId);
            return GamePendingProgressOutcome.NoChange;
        }

        try
        {
            foreach (GameParticipant participant in pending)
                await ScoreAndAdvanceAsync(game, participant, gameMode.Key, cancellationToken);

            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        }
        catch (GameProblemPositionConflictException ex)
        {
            LogPositionConflict(ex, game.Id);
            await gameWriteRepository.ReloadAsync(game.Id, cancellationToken);
            return GamePendingProgressOutcome.ConflictedNeedsReload;
        }

        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
        return GamePendingProgressOutcome.Applied;
    }

    private static Result<GameSubmissionProgressResult> GameNotFound(Guid gameId) =>
        Result<GameSubmissionProgressResult>.NotFound($"Game '{gameId}' was not found.");

    private static bool IsStillOnSubmittedProblem(GameParticipant participant, GameProblemSubmissionDto submission) =>
        participant.ProblemSession != null && participant.ProblemSession.CurrentProblemId == submission.ProblemId;

    private static bool HasUnscoredSolve(
        GameParticipant participant,
        IReadOnlyList<GameProblemSubmissionDto> latestSubmissions
    )
    {
        if (participant.ProblemSession is null)
            return false;

        Guid currentProblemId = participant.ProblemSession.CurrentProblemId;

        GameProblemSubmissionDto? latest = latestSubmissions.FirstOrDefault(s =>
            s.UserId == participant.UserId && s.ProblemId == currentProblemId
        );

        if (latest is null || latest.Status != SubmissionStatus.Accepted)
            return false;

        return participant.CanAttemptProblem(ParticipantAction.CompleteProblem, currentProblemId)
            == ProblemAttemptEligibility.Eligible;
    }

    private async Task<GameMode?> ResolveGameModeBeforeMutatingAsync(Game game, CancellationToken cancellationToken) =>
        await gameReadRepository.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, cancellationToken);

    private async Task<Guid?> ScoreAndAdvanceAsync(
        Game game,
        GameParticipant participant,
        string gameModeKey,
        CancellationToken cancellationToken
    )
    {
        game.RecordProblemSolved(participant.UserId);

        return await gameProblemAdvancer.AdvanceAsync(
            game,
            participant,
            gameModeKey,
            onAdvance: participant.AdvanceProblem,
            onFinish: null,
            cancellationToken
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

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
    }

    private static Result<GameSubmissionProgressResult> NotApplicable(int? score) =>
        Result<GameSubmissionProgressResult>.Success(
            new GameSubmissionProgressResult(GameSubmissionProgressOutcome.NotApplicable, score, null)
        );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Submission {SubmissionId} records game {GameId} for user {UserId}, who is not a participant in it; the data is inconsistent and no progress can be applied."
    )]
    private partial void LogParticipantMissing(Guid submissionId, Guid gameId, Guid userId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Game {GameId} references mode {GameModeId} which no longer exists; cannot apply pending submission progress."
    )]
    private partial void LogGameModeMissing(Guid gameId, Guid gameModeId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Lost the problem-position race while applying pending submission progress for game {GameId}; the next game read or the completion event will apply it."
    )]
    private partial void LogPositionConflict(Exception ex, Guid gameId);
}
