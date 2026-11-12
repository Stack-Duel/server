namespace StackDuel.Application.Games.Dtos;

public sealed record PlayerGameRatingInputDto(
    Guid PlayerId,
    int PlayerCurrentRating,
    int GameScore
);