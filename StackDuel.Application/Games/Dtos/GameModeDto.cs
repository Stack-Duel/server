namespace StackDuel.Application.Games.Dtos;

public sealed record GameModeTimeOptionDto(int DurationSeconds, bool IsDefault);

public sealed record GameModeDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    int MinPlayers,
    int MaxPlayers,
    IReadOnlyCollection<GameModeTimeOptionDto> TimeOptions
);