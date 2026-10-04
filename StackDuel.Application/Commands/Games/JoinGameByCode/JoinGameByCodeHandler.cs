using Ardalis.Result;
using FluentValidation;
using MediatR;
using StackDuel.Application.Commands.Games.JoinGame;
using StackDuel.Application.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.JoinGameByCode;

internal sealed class JoinGameByCodeHandler(
    IGameReadRepository gameReadRepository,
    IMediator mediator,
    IValidator<JoinGameByCodeCommand> validator
) : AbstractCommandHandler<JoinGameByCodeCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        JoinGameByCodeCommand request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByJoinCodeAsync(request.JoinCode, cancellationToken);

        if (game is null)
            return Result<Guid>.NotFound("Invalid or expired code.");

        // Already in this game (e.g. the host re-using their own invite, or someone
        // revisiting a link they already joined from) — send them back in rather than
        // erroring, regardless of the game's current status.
        if (game.Participants.Any(p => p.UserId == request.RequestedByUserId))
            return Result<Guid>.Success(game.Id);

        if (game.Status != GameStatus.Pending)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(game.Status), "This game is no longer accepting new players.")
            );

        Result joinResult = await mediator.Send(
            new JoinGameCommand(game.Id, request.RequestedByUserId),
            cancellationToken
        );

        if (!joinResult.IsSuccess)
            return joinResult;

        return Result<Guid>.Success(game.Id);
    }
}