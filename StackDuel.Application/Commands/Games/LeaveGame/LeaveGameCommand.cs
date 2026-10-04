namespace StackDuel.Application.Commands.Games.LeaveGame;

internal sealed record LeaveGameCommand(Guid GameId, Guid RequestedByUserId) : ICommand;