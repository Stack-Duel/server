namespace StackDuel.Application.Games;

internal static class GameModeStartCountdown
{
    private const int MultiplayerCountdownSeconds = 6;

    public static int SecondsFor(string gameModeKey) =>
        gameModeKey switch
        {
            "duel" or "ffa" => MultiplayerCountdownSeconds,
            _ => 0,
        };
}