using StackDuel.Application.Notifications;
using StackDuel.Domain.Games.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Events.Games;

internal sealed partial class GameLobbyUpdatedDomainEventHandler(
    IGameNotificationService gameNotificationService,
    ILogger<GameLobbyUpdatedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<GameLobbyUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<GameLobbyUpdatedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;

        try
        {
            await gameNotificationService.NotifyGameLobbyUpdatedAsync(e.GameId, cancellationToken);
        }
        catch (Exception ex)
        {
            LogNotifyFailed(ex, e.GameId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to push lobby-updated notification for game {GameId}; clients will pick up the new roster on their next poll/fetch."
    )]
    private partial void LogNotifyFailed(Exception ex, Guid gameId);
}