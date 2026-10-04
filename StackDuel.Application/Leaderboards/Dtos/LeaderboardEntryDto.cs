namespace StackDuel.Application.Leaderboards.Dtos;

public sealed record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string Username,
    string? ImageUrl,
    int HighScore,
    bool IsCurrentUser
);