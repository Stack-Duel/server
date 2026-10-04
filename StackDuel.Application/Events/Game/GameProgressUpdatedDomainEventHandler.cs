using StackDuel.Application.Notifications;
using StackDuel.Domain.Games.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Events.Games;

internal sealed partial class GameProgressUpdatedDomainEventHandler(
    IGameNotificationService gameNotificationService,
    ILogger<GameProgressUpdatedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<GameProgressUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<GameProgressUpdatedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;

        try
        {
            await gameNotificationService.NotifyGameProgressUpdatedAsync(e.GameId, cancellationToken);
        }
        catch (Exception ex)
        {
            LogNotifyFailed(ex, e.GameId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to push progress-updated notification for game {GameId}; clients will pick up the new score on their next poll/fetch."
    )]
    private partial void LogNotifyFailed(Exception ex, Guid gameId);
}