namespace StackDuel.Application.Games.Dtos;

public sealed record CalculatedPlayerRankingScoreDto(
    Guid PlayerId,
    int PlayerRating
);