namespace StackDuel.Application.Commands.Games.ForfeitGame;

internal sealed record ForfeitGameCommand(Guid GameId, Guid RequestedByUserId) : ICommand;