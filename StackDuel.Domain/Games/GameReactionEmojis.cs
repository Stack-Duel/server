namespace StackDuel.Domain.Games;

/// <summary>
/// Allow-list of emoji a player can send as a quick reaction during a Running game. Single
/// source of truth for server-side validation — the frontend keeps its own matching display list
/// (web/src/domains/game/ramp/quick-reactions.ts) since there's no shared package between the two
/// stacks; growing this set is just appending an entry to both.
/// </summary>
public static class GameReactionEmojis
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>
    {
        "👍",
        "🔥",
        "😂",
        "😮",
        "💪",
        "🎉",
        "😅",
        "🤝",
    };
}