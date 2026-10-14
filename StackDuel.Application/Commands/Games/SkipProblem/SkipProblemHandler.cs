using StackDuel.Domain.SeedWork;
using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.SkipProblem;

internal sealed class SkipProblemHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IGameProblemAdvancer gameProblemAdvancer,
    IValidator<SkipProblemCommand> validator
) : AbstractCommandHandler<SkipProblemCommand, SkipProblemResultDto>(validator)
{
    protected override async Task<Result<SkipProblemResultDto>> HandleValidated(
        SkipProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        int saveAttempt = 0;
        while (true)
        {
            var game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
            if (game is null)
                return Result<SkipProblemResultDto>.NotFound($"Game '{request.GameId}' was not found.");

            var attempt = game.EvaluateProblemAttempt(
                request.RequestedByUserId,
                request.ProblemId,
                ParticipantAction.SkipProblem,
                DateTime.UtcNow
            );

            if (attempt.GameJustExpired)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
            }

            if (!attempt.IsEligible)
                return GameProblemAttemptResult.ToFailure<SkipProblemResultDto>(attempt, ParticipantAction.SkipProblem);

            var participant = attempt.Participant!;

            if (!game.SkipsEnabled)
                return Result<SkipProblemResultDto>.Invalid(
                    new ValidationError(nameof(game.SkipsEnabled), "Skips are not enabled for this game.")
                );

            if (participant.SkipsRemaining <= 0)
                return Result<SkipProblemResultDto>.Invalid(
                    new ValidationError(nameof(participant.SkipsRemaining), "No skips remaining.")
                );

            var gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
            if (gameMode is null)
                return Result<SkipProblemResultDto>.NotFound("Game references a mode that no longer exists.");

            Guid? nextProblemId;
            try
            {
                nextProblemId = await gameProblemAdvancer.AdvanceAsync(
                    game,
                    participant,
                    gameMode.Key,
                    onAdvance: nextProblemId =>
                        game.SkipCurrentRoundAndAdvance(request.RequestedByUserId, nextProblemId),
                    onFinish: participant.UseSkip,
                    cancellationToken
                );
            }
            catch (DuplicateGameProblemException)
            {
                return Result<SkipProblemResultDto>.Error("Failed to advance to the next problem.");
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

            return Result<SkipProblemResultDto>.Success(
                new SkipProblemResultDto(participant.SkipsRemaining, nextProblemId)
            );
        }
    }
}