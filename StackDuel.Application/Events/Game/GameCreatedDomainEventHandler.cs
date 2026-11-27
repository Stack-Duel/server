using MediatR;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Games.Events;

namespace StackDuel.Application.Events.Games;

internal sealed partial class GameCreatedDomainEventHandler(
    IMessagePublisher messagePublisher,
    ILogger<GameCreatedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<GameCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<GameCreatedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        Guid gameId = notification.DomainEvent.GameId;

        try
        {
            await messagePublisher.PublishAsync(new GameCreatedMessage(gameId), cancellationToken);
        }
        catch (Exception ex)
        {
            LogPublishFailed(ex, gameId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish GameCreatedMessage for game {GameId}; the game was still created but will not be announced."
    )]
    private partial void LogPublishFailed(Exception ex, Guid gameId);
}