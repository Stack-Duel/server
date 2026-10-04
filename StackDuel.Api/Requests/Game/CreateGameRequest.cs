namespace StackDuel.Api.Requests.Game;

public sealed record TrackSelectionRequest(string TrackKey, IReadOnlyList<Guid> LanguageIds);

public sealed record CreateGameRequest(
    string GameModeKey,
    IReadOnlyList<TrackSelectionRequest> TrackSelections,
    int TimeLimitInSeconds,
    bool SkipsEnabled = true
);