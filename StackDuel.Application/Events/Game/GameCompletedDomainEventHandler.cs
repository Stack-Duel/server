using StackDuel.Application.Notifications;
using StackDuel.Domain.Games.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Events.Games;

internal sealed partial class GameCompletedDomainEventHandler(
    IGameNotificationService gameNotificationService,
    ILogger<GameCompletedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<GameCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<GameCompletedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;

        try
        {
            await gameNotificationService.NotifyGameCompletedAsync(e.GameId, e.Status, e.EndedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            // Push is purely a latency optimization over the client's own polling/re-fetch, so a
            // failure here must never fault the command that finalized the game.
            LogNotifyFailed(ex, e.GameId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to push game-completed notification for game {GameId}; clients will pick up the new state on their next poll/fetch."
    )]
    private partial void LogNotifyFailed(Exception ex, Guid gameId);
}