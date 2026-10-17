using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Games;

namespace StackDuel.Application.Commands.Games.JoinGame;

internal sealed class JoinGameHandler(IGameJoiner gameJoiner, IValidator<JoinGameCommand> validator)
    : AbstractCommandHandler<JoinGameCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        JoinGameCommand request,
        CancellationToken cancellationToken
    ) => await gameJoiner.JoinAsync(request.GameId, request.RequestedByUserId, cancellationToken);
}