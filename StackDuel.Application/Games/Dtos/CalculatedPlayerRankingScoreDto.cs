namespace StackDuel.Application.Games.Dtos;

public sealed record CalculatedPlayerRankingScoreDto(
    Guid PlayerId,
    int PlayerRating,
    int PlayerRatingChange,
    int NewPlayerRating
);