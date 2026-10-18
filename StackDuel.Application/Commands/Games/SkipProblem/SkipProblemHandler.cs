using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Commands.Games.SkipProblem;

internal sealed class SkipProblemHandler(
    IGameReadRepository gameReadRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IGameProblemAdvancer gameProblemAdvancer,
    IGameConcurrencyGuard gameConcurrencyGuard,
    IValidator<SkipProblemCommand> validator
) : AbstractCommandHandler<SkipProblemCommand, SkipProblemResultDto>(validator)
{
    protected override async Task<Result<SkipProblemResultDto>> HandleValidated(
        SkipProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await gameConcurrencyGuard.RunExclusiveAsync(
                request.GameId,
                async ct =>
                {
                    var game = await gameReadRepository.FindGameByIdAsync(request.GameId, ct);
                    if (game is null)
                        return Result<SkipProblemResultDto>.NotFound($"Game '{request.GameId}' was not found.");

                    Result<SkipProblemResultDto> result = await EvaluateAndAdvanceAsync(game, request, ct);

                    await unitOfWork.SaveChangesAsync(ct);
                    await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), ct);

                    return result;
                },
                cancellationToken
            );
        }
        catch (DuplicateGameProblemException)
        {
            return Result<SkipProblemResultDto>.Error("Failed to advance to the next problem.");
        }
    }

    private async Task<Result<SkipProblemResultDto>> EvaluateAndAdvanceAsync(
        Game game,
        SkipProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        var attempt = game.EvaluateProblemAttempt(
            request.RequestedByUserId,
            request.ProblemId,
            ParticipantAction.SkipProblem,
            DateTime.UtcNow
        );

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

        game.SkipCurrentRound(request.RequestedByUserId);

        Guid? nextProblemId = await gameProblemAdvancer.AdvanceAsync(
            game,
            request.RequestedByUserId,
            gameMode.Key,
            cancellationToken
        );

        return Result<SkipProblemResultDto>.Success(new SkipProblemResultDto(participant.SkipsRemaining, nextProblemId));
    }
}