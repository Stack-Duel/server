using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.StartGame;

internal sealed record StartGameCommand(Guid GameId, Guid RequestedByUserId) : ICommand;