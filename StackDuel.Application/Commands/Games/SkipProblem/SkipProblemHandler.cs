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
        var game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
        if (game is null)
            return GameNotFound(request.GameId);

        int saveAttempt = 0;
        while (true)
        {
            var attempt = game.EvaluateProblemAttempt(
                request.RequestedByUserId,
                request.ProblemId,
                ParticipantAction.SkipProblem,
                DateTime.UtcNow
            );

            if (attempt.GameJustExpired)
            {
                await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
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

            Guid? nextProblemId = await gameProblemAdvancer.AdvanceAsync(
                game,
                participant,
                gameMode.Key,
                onAdvance: participant.SkipToProblem,
                onFinish: participant.UseSkip,
                cancellationToken
            );

            try
            {
                await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            }
            catch (GameProblemPositionConflictException) when (saveAttempt == 0)
            {
                saveAttempt++;

                game = await gameWriteRepository.ReloadAsync(request.GameId, cancellationToken);
                if (game is null)
                    return GameNotFound(request.GameId);

                continue;
            }

            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

            return Result<SkipProblemResultDto>.Success(
                new SkipProblemResultDto(participant.SkipsRemaining, nextProblemId)
            );
        }
    }

    private static Result<SkipProblemResultDto> GameNotFound(Guid gameId) =>
        Result<SkipProblemResultDto>.NotFound($"Game '{gameId}' was not found.");
}