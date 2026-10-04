namespace StackDuel.Application.Commands.Games.CloseLobby;

internal sealed record CloseLobbyCommand(Guid GameId, Guid RequestedByUserId) : ICommand;