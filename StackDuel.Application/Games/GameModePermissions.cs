using StackDuel.Domain.Authorization.Rbac;

namespace StackDuel.Application.Games;

internal static class GameModePermissions
{
    public static string? For(string gameModeKey) =>
        gameModeKey switch
        {
            "solo_rush" => WellKnownAuthorization.PlaySoloRushPermission,
            "duel" => WellKnownAuthorization.PlayDuelPermission,
            "ffa" => WellKnownAuthorization.PlayFfaPermission,
            _ => null,
        };
}