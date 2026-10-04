namespace StackDuel.Application.Games;

public sealed record TrackLanguageSelection(string TrackKey, IReadOnlyList<Guid> LanguageIds);