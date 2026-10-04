using Ardalis.Result;
using FluentValidation;
using MediatR;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.ForfeitGame;

internal sealed class ForfeitGameHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IGameExpiryCanceller gameExpiryCanceller,
    IValidator<ForfeitGameCommand> validator,
    IDomainEventDispatcher domainEventDispatcher
) : AbstractCommandHandler<ForfeitGameCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        ForfeitGameCommand request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result.NotFound($"Game '{request.GameId}' was not found.");

        GameParticipant? participant = game.Participants.FirstOrDefault(p => p.UserId == request.RequestedByUserId);
        if (participant is null)
            return Result.Forbidden();

        if (game.Status != GameStatus.Running)
            return Result.Invalid(new ValidationError(nameof(game.Status), "Only running games can be forfeited."));

        if (!participant.CanPerform(ParticipantAction.Forfeit))
            return Result.Invalid(
                new ValidationError(
                    nameof(request.RequestedByUserId),
                    participant.HasForfeited
                        ? "You have already forfeited this game."
                        : "You have already completed all available problems."
                )
            );

        game.Forfeit(request.RequestedByUserId);

        // Only the actual game-ending case (every participant has now stopped playing) needs the
        // scheduled expiry cancelled — while other participants are still playing, the timer is
        // still needed to end the game normally for them when time runs out.
        await gameExpiryCanceller.CancelIfScheduledAsync(game, cancellationToken);

        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result.Success();
    }
}