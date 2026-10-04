using StackDuel.Application.Games;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Events.Games;

internal sealed partial class GameStartedDomainEventHandler(
    IMessagePublisher messagePublisher,
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    ILogger<GameStartedDomainEventHandler> logger
) : INotificationHandler<DomainEventNotification<GameStartedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<GameStartedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;

        DateTimeOffset scheduledEnqueueTimeUtc = e.StartedAt.AddSeconds(e.TimeLimitInSeconds);

        long sequenceNumber;
        try
        {
            sequenceNumber = await messagePublisher.PublishScheduledAsync(
                new GameTimeExpiredMessage(e.GameId, e.StartedAt, RescheduleCount: 0),
                scheduledEnqueueTimeUtc,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            // If scheduling fails, the game still gets finalized eventually by the sweep job
            // backstop — log loudly so it's visible, but don't fault game-start over it.
            LogScheduleFailed(ex, e.GameId);
            return;
        }

        // 0 is the RabbitMQ placeholder (no real handle exists there) — only persist a
        // meaningful handle for transports that can actually use it to cancel later.
        if (sequenceNumber == 0)
            return;

        Game? game = await gameReadRepository.FindGameByIdAsync(e.GameId, cancellationToken);
        if (game is null)
            return;

        game.AttachScheduledExpiry(sequenceNumber);
        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to schedule GameTimeExpiredMessage for game {GameId}; the sweep job will finalize it if scheduling never recovers."
    )]
    private partial void LogScheduleFailed(Exception ex, Guid gameId);
}