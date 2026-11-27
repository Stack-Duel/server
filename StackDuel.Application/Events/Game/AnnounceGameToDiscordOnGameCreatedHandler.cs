using MediatR;
using Microsoft.Extensions.Logging;
using StackDuel.Domain.Games.Events;

namespace StackDuel.Application.Events.Game;

internal sealed partial class AnnounceGameToDiscordOnGameCreatedHandler(
    ILogger<AnnounceGameToDiscordOnGameCreatedHandler> logger
) : INotificationHandler<DomainEventNotification<GameCreatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<GameCreatedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        Guid gameId = notification.DomainEvent.GameId;

        try
        {
            LogAnnouncementPending(gameId);
        }
        catch (Exception ex)
        {
            LogAnnounceFailed(ex, gameId);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId} was created; no Discord announcer is wired up yet.")]
    private partial void LogAnnouncementPending(Guid gameId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to announce game {GameId} to Discord; the game was still created."
    )]
    private partial void LogAnnounceFailed(Exception ex, Guid gameId);
}