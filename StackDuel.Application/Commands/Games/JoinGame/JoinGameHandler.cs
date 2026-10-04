using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Games.JoinGame;

internal sealed class JoinGameHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    UserContext userContext,
    IDomainEventDispatcher domainEventDispatcher,
    IValidator<JoinGameCommand> validator
) : AbstractCommandHandler<JoinGameCommand>(validator)
{
    protected override async Task<Result> HandleValidated(JoinGameCommand request, CancellationToken cancellationToken)
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result.NotFound($"Game '{request.GameId}' was not found.");

        if (game.Status != GameStatus.Pending)
            return Result.Invalid(new ValidationError(nameof(game.Status), "Only pending games can be joined."));

        GameMode? gameMode = await gameReadRepository.FindGameModeByIdAsync(game.GameModeId, cancellationToken);
        if (gameMode is null)
            return Result.NotFound($"Game mode '{game.GameModeId}' was not found.");

        string? requiredPermission = GameModePermissions.For(gameMode.Key);
        bool hasAccess = requiredPermission is not null && userContext.HasPermission(requiredPermission);

        if (!hasAccess)
            return Result.Forbidden();

        if (game.Participants.Any(p => p.UserId == request.RequestedByUserId))
            return Result.Invalid(
                new ValidationError(nameof(request.RequestedByUserId), "You have already joined this game.")
            );

        if (game.Participants.Count >= gameMode.MaxPlayers)
            return Result.Invalid(new ValidationError(nameof(gameMode.MaxPlayers), "This game is already full."));

        game.Join(request.RequestedByUserId, gameMode.MaxPlayers);

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result.Success();
    }
}