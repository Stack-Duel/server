using StackDuel.Application.Games.Dtos;


namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

public sealed record CalculatePlayerRankingsFromGameCommand(
    Guid GameId
    ) : ICommand<IEnumerable<CalculatedPlayerRankingScoreDto>>;
