using MediatR;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Notifications;
using StackDuel.Application.Services.Games;
using StackDuel.Domain.Games.Events;

namespace StackDuel.Application.Events.Games;

internal sealed partial class GameCompletedDomainEventHandler(
    IGameNotificationService gameNotificationService,
    IPlayerGameRankingService playerGameRankingService,
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
            await playerGameRankingService.CalculatePlayerGameRankingAsync(e.GameId, cancellationToken);
            await gameNotificationService.NotifyGameCompletedAsync(e.GameId, e.Status, e.EndedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            LogNotifyFailed(ex, e.GameId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to push game-completed notification for game {GameId}; clients will pick up the new state on their next poll/fetch."
    )]
    private partial void LogNotifyFailed(Exception ex, Guid gameId);
}