using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games.Dtos;

public sealed record MyActiveGameDto(
    Guid GameId,
    string GameModeKey,
    string GameModeName,
    GameStatus Status,
    int TimeLimitInSeconds,
    DateTime CreatedAt,
    DateTime? StartedAt,
    int ParticipantCount,
    int MaxPlayers,
    bool IsHost,
    string HostUsername,
    IReadOnlyList<string> TechStacks
);