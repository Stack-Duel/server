namespace StackDuel.Application.Notifications;

/// <summary>
/// Shape pushed to clients over the game hub when a Running game's progress changes (someone
/// solved a problem). Same "just a re-fetch signal" treatment as GameCompletedNotification /
/// GameLobbyUpdatedNotification — the client reads the actual score back from the REST API.
/// </summary>
public sealed record GameProgressUpdatedNotification(Guid GameId);