using Microsoft.AspNetCore.SignalR;
using StackDuel.Application.Notifications;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Api.Hubs;

internal sealed class SignalRGameNotificationService(IHubContext<GameHub> hubContext) : IGameNotificationService
{
    public const string GameCompletedMethod = "GameCompleted";
    public const string GameLobbyUpdatedMethod = "GameLobbyUpdated";
    public const string GameProgressUpdatedMethod = "GameProgressUpdated";
    public const string GameParticipantAttemptedMethod = "GameParticipantAttempted";
    public const string GameParticipantReactedMethod = "GameParticipantReacted";

    public Task NotifyGameCompletedAsync(
        Guid gameId,
        GameStatus status,
        DateTime endedAt,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new GameCompletedNotification(gameId, status, endedAt);

        return hubContext
            .Clients.Group(GameHub.GroupNameFor(gameId))
            .SendAsync(GameCompletedMethod, payload, cancellationToken);
    }

    public Task NotifyGameLobbyUpdatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var payload = new GameLobbyUpdatedNotification(gameId);

        return hubContext
            .Clients.Group(GameHub.GroupNameFor(gameId))
            .SendAsync(GameLobbyUpdatedMethod, payload, cancellationToken);
    }

    public Task NotifyGameProgressUpdatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var payload = new GameProgressUpdatedNotification(gameId);

        return hubContext
            .Clients.Group(GameHub.GroupNameFor(gameId))
            .SendAsync(GameProgressUpdatedMethod, payload, cancellationToken);
    }

    public Task NotifyGameParticipantAttemptedAsync(
        Guid gameId,
        Guid userId,
        SubmissionStatus status,
        DateTime attemptedAt,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new GameParticipantAttemptedNotification(gameId, userId, status, attemptedAt);

        return hubContext
            .Clients.Group(GameHub.GroupNameFor(gameId))
            .SendAsync(GameParticipantAttemptedMethod, payload, cancellationToken);
    }

    public Task NotifyGameParticipantReactedAsync(
        Guid gameId,
        Guid userId,
        string emoji,
        DateTime sentAt,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new GameParticipantReactedNotification(gameId, userId, emoji, sentAt);

        return hubContext
            .Clients.Group(GameHub.GroupNameFor(gameId))
            .SendAsync(GameParticipantReactedMethod, payload, cancellationToken);
    }
}