
namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

public sealed record CalculatePlayerRankingsFromGameCommand(Guid GameId) : ICommand;
