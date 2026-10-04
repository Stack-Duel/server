using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.JoinGame;

internal sealed record JoinGameCommand(Guid GameId, Guid RequestedByUserId) : ICommand;