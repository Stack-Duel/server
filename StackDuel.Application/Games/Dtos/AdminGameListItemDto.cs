using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games.Dtos;

public sealed record AdminGameParticipantSummaryDto(
    Guid UserId,
    string Username,
    int Score,
    bool HasForfeited,
    bool HasFinishedProblems
);

public sealed record AdminGameListItemDto(
    Guid GameId,
    string GameModeKey,
    string GameModeName,
    GameStatus Status,
    int TimeLimitInSeconds,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<AdminGameParticipantSummaryDto> Participants
);