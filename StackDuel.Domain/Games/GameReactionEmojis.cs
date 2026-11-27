namespace StackDuel.Domain.Games;

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