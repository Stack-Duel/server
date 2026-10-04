namespace StackDuel.Application.Tracks.Dtos;

public sealed record TrackLanguageDto(Guid Id, string Name);

public sealed record TrackDto(
    Guid Id,
    string Key,
    string Name,
    bool AllowsLanguageSelection,
    IReadOnlyList<TrackLanguageDto> Languages
);