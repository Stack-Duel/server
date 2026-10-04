namespace StackDuel.Application.Games.Dtos;

public sealed record GameLobbySummaryDto(
    Guid GameId,
    string GameModeKey,
    string GameModeName,
    int TimeLimitInSeconds,
    DateTime CreatedAt,
    int MinPlayers,
    int MaxPlayers,
    int ParticipantCount,
    string HostUsername,
    bool IsHost,
    bool IsParticipant,
    IReadOnlyList<string> TechStacks
);