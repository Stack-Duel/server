using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.LeaveGame;

internal sealed class LeaveGameHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    IValidator<LeaveGameCommand> validator
) : AbstractCommandHandler<LeaveGameCommand>(validator)
{
    protected override async Task<Result> HandleValidated(LeaveGameCommand request, CancellationToken cancellationToken)
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result.NotFound($"Game '{request.GameId}' was not found.");

        bool isParticipant = game.Participants.Any(p => p.UserId == request.RequestedByUserId);
        if (!isParticipant)
            return Result.Forbidden();

        if (game.Status != GameStatus.Pending)
            return Result.Invalid(
                new ValidationError(
                    nameof(game.Status),
                    "Only a pending lobby can be left — once a game has started, forfeit instead."
                )
            );

        game.Leave(request.RequestedByUserId);

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result.Success();
    }
}