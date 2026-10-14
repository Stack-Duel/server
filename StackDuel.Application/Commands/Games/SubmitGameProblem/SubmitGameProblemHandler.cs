using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Commands.Submissions.CreateSubmission;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Problems;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Commands.Games.SubmitGameProblem;

internal sealed partial class SubmitGameProblemHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IGameRoundSubmissionWriteRepository gameRoundSubmissionWriteRepository,
    IProblemReadRepository problemReadRepository,
    IDomainEventDispatcher domainEventDispatcher,
    IMediator mediator,
    IValidator<SubmitGameProblemCommand> validator,
    ILogger<SubmitGameProblemHandler> logger
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

        Guid? currentProblemId = game.CurrentProblemIdFor(participant.UserId);
        if (currentProblemId is null)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(currentProblemId), "Round not started.")
            );

        if (currentProblemId != request.ProblemId)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(request.ProblemId),
                    "The specified problem is not the participant's current problem."
                )
            );

        // The client picks which setup it ran the code against (e.g. language version); verify it
        // actually belongs to the problem being submitted rather than trusting it blindly. Without
        // this, a mismatch would only surface once game_round_submissions' composite foreign keys
        // reject the insert below, turning a bad request into a mid-transaction 500.
        Guid? setupProblemId = await problemReadRepository.GetProblemIdByProblemSetupIdAsync(
            request.ProblemSetupId,
            cancellationToken
        );
        if (setupProblemId != request.ProblemId)
            return Result<Guid>.Invalid(
                new ValidationError(
                    nameof(request.ProblemSetupId),
                    "The specified problem setup does not belong to the current problem."
                )
            );

        // Resolved up front, not after the submission exists: a missing round is a hard failure, and
        // failing here means nothing has been written and no code has been queued to execute. Doing
        // this lookup later would report failure for a submission that is already running.
        Guid? roundId = await gameReadRepository.FindRoundIdByGameAndProblemAsync(
            game.Id,
            request.ProblemId,
            cancellationToken
        );
        if (roundId is null)
            return Result<Guid>.Error("Game round not found for the current problem.");

        // Create the grade submission through the existing submission pipeline.
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

        // Stamp the submission ID on the round so completeProblem can verify it came from here.
        game.AttachSubmissionFor(request.RequestedByUserId, submissionResult.Value);
        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);

        await RecordRoundSubmissionAsync(
            roundId.Value,
            participant.Id,
            request,
            submissionResult.Value,
            game.Id,
            participant.UserId,
            cancellationToken
        );

        return Result<Guid>.Success(submissionResult.Value);
    }

    /// <summary>
    /// Mirrors the accepted submission into <c>game_round_submissions</c>, the relational successor
    /// to the <c>problem_session</c> jsonb. Nothing reads that table yet, so a failure here must not
    /// fail a submission that has already been created and queued for execution — it is logged and
    /// swallowed, leaving only a gap in attempt history. Once reads move over, this write belongs in
    /// the same transaction as the game save and a failure should become fatal.
    /// </summary>
    private async Task RecordRoundSubmissionAsync(
        Guid roundId,
        Guid participantId,
        SubmitGameProblemCommand request,
        Guid submissionId,
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            GameRoundSubmission roundSubmission = new(
                roundId,
                participantId,
                request.ProblemSetupId,
                submissionId,
                request.ProblemId,
                gameId,
                userId
            );

            await gameRoundSubmissionWriteRepository.AddAsync(roundSubmission, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRoundSubmissionFailed(exception, submissionId, roundId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to record submission {SubmissionId} against round {RoundId}; the submission itself was accepted, so attempt history for this round is incomplete."
    )]
    private partial void LogRoundSubmissionFailed(Exception exception, Guid submissionId, Guid roundId);
}