namespace StackDuel.Application.Notifications;

/// <summary>
/// Shape pushed to clients over the game hub when a Pending lobby's participant roster changes.
/// Deliberately just a "go re-fetch" signal, like GameCompletedNotification — the client reads
/// the actual roster back from the REST API rather than trusting anything more off the socket.
/// </summary>
public sealed record GameLobbyUpdatedNotification(Guid GameId);