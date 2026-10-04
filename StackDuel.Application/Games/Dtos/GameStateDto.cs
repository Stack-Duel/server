using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games.Dtos;

public sealed record GameParticipantDto(
    Guid UserId,
    string Username,
    string? ImageUrl,
    int SeatNumber,
    DateTime JoinedAt,
    int Score = 0,
    GameCurrentProblemDto? CurrentProblem = null,
    bool HasForfeited = false,
    bool HasFinishedProblems = false,
    int SkipsRemaining = 0
);

public sealed record GameStateDto(
    Guid GameId,
    Guid GameModeId,
    string GameModeKey,
    GameStatus Status,
    int TimeLimitInSeconds,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyCollection<GameParticipantDto> Participants,
    IReadOnlyList<string> TechStacks,
    string JoinCode,
    bool SkipsEnabled = true
);