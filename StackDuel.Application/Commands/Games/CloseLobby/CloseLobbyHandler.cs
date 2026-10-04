using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.CloseLobby;

internal sealed class CloseLobbyHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    IValidator<CloseLobbyCommand> validator
) : AbstractCommandHandler<CloseLobbyCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        CloseLobbyCommand request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result.NotFound($"Game '{request.GameId}' was not found.");

        bool isHost = game.HostUserId == request.RequestedByUserId;
        if (!isHost)
            return Result.Forbidden();

        if (game.Status != GameStatus.Pending)
            return Result.Invalid(
                new ValidationError(
                    nameof(game.Status),
                    "Only a pending lobby can be closed — once the game has started, forfeit instead."
                )
            );

        game.CloseLobby();

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result.Success();
    }
}