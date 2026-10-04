using Ardalis.Result;
using FluentValidation;
using MediatR;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.StartGame;

internal sealed class StartGameHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IProblemSelectionStrategyResolver problemSelectionStrategyResolver,
    IGameProblemSequencer gameProblemSequencer,
    IDomainEventDispatcher domainEventDispatcher,
    IValidator<StartGameCommand> validator
) : AbstractCommandHandler<StartGameCommand>(validator)
{
    protected override async Task<Result> HandleValidated(StartGameCommand request, CancellationToken cancellationToken)
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result.NotFound($"Game '{request.GameId}' was not found.");

        bool isHost = game.HostUserId == request.RequestedByUserId;
        if (!isHost)
            return Result.Forbidden();

        if (game.Status != GameStatus.Pending)
            return Result.Invalid(new ValidationError(nameof(game.Status), "Only pending games can be started."));

        var gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
        if (gameMode is null)
            return Result.NotFound($"Game mode '{game.GameModeId}' was not found.");

        if (game.Participants.Count < gameMode.MinPlayers)
            return Result.Invalid(
                new ValidationError(
                    nameof(gameMode.MinPlayers),
                    $"At least {gameMode.MinPlayers} players are required to start this game."
                )
            );

        game.Start(GameModeStartCountdown.SecondsFor(gameMode.Key));

        var problemSelectionStrategy = problemSelectionStrategyResolver.Resolve(gameMode.Key);
        if (problemSelectionStrategy is null)
            return Result.NotFound($"Problem selection strategy for '{gameMode.Key}' was not found.");

        Guid? initialProblemId = await gameProblemSequencer.GetOrGenerateProblemAsync(
            game,
            0,
            gameMode.Key,
            problemSelectionStrategy,
            cancellationToken
        );

        if (initialProblemId is null)
            return Result.Invalid(
                new ValidationError(nameof(initialProblemId), "Could not select an initial problem.")
            );

        foreach (var participant in game.Participants)
        {
            participant.InitializeProblemSession(initialProblemId.Value);
        }

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result.Success();
    }
}