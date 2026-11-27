using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Events.Games;
using StackDuel.Application.Notifications;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Games.Events;

namespace StackDuel.Application.Tests.Events.Games;

public class GameNotificationDomainEventHandlerTests
{
    private readonly Mock<IGameNotificationService> _notifications = new();
    private readonly Guid _gameId = Guid.NewGuid();

    [Fact]
    public async Task GameCompletedHandler_PushesTheCompletionToWhoeverIsWatching()
    {
        DateTime endedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        GameCompletedDomainEventHandler handler = new(
            _notifications.Object,
            NullLogger<GameCompletedDomainEventHandler>.Instance
        );

        await handler.Handle(
            new DomainEventNotification<GameCompletedDomainEvent>(
                new GameCompletedDomainEvent(_gameId, GameStatus.Completed, endedAt)
            ),
            CancellationToken.None
        );

        _notifications.Verify(
            x => x.NotifyGameCompletedAsync(_gameId, GameStatus.Completed, endedAt),
            Times.Once
        );
    }

    [Fact]
    public async Task GameCompletedHandler_SwallowsAPushFailure()
    {
        _notifications
            .Setup(x => x.NotifyGameCompletedAsync(It.IsAny<Guid>(), It.IsAny<GameStatus>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));
        GameCompletedDomainEventHandler handler = new(
            _notifications.Object,
            NullLogger<GameCompletedDomainEventHandler>.Instance
        );

        await handler.Handle(
            new DomainEventNotification<GameCompletedDomainEvent>(
                new GameCompletedDomainEvent(_gameId, GameStatus.Completed, DateTime.UtcNow)
            ),
            CancellationToken.None
        );
    }

    [Fact]
    public async Task GameLobbyUpdatedHandler_PushesTheLobbyUpdate()
    {
        GameLobbyUpdatedDomainEventHandler handler = new(
            _notifications.Object,
            NullLogger<GameLobbyUpdatedDomainEventHandler>.Instance
        );

        await handler.Handle(
            new DomainEventNotification<GameLobbyUpdatedDomainEvent>(new GameLobbyUpdatedDomainEvent(_gameId)),
            CancellationToken.None
        );

        _notifications.Verify(x => x.NotifyGameLobbyUpdatedAsync(_gameId), Times.Once);
    }

    [Fact]
    public async Task GameLobbyUpdatedHandler_SwallowsAPushFailure()
    {
        _notifications
            .Setup(x => x.NotifyGameLobbyUpdatedAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));
        GameLobbyUpdatedDomainEventHandler handler = new(
            _notifications.Object,
            NullLogger<GameLobbyUpdatedDomainEventHandler>.Instance
        );

        await handler.Handle(
            new DomainEventNotification<GameLobbyUpdatedDomainEvent>(new GameLobbyUpdatedDomainEvent(_gameId)),
            CancellationToken.None
        );
    }

    [Fact]
    public async Task GameProgressUpdatedHandler_PushesTheProgressUpdate()
    {
        GameProgressUpdatedDomainEventHandler handler = new(
            _notifications.Object,
            NullLogger<GameProgressUpdatedDomainEventHandler>.Instance
        );

        await handler.Handle(
            new DomainEventNotification<GameProgressUpdatedDomainEvent>(new GameProgressUpdatedDomainEvent(_gameId)),
            CancellationToken.None
        );

        _notifications.Verify(x => x.NotifyGameProgressUpdatedAsync(_gameId), Times.Once);
    }

    [Fact]
    public async Task GameProgressUpdatedHandler_SwallowsAPushFailure()
    {
        _notifications
            .Setup(x => x.NotifyGameProgressUpdatedAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));
        GameProgressUpdatedDomainEventHandler handler = new(
            _notifications.Object,
            NullLogger<GameProgressUpdatedDomainEventHandler>.Instance
        );

        await handler.Handle(
            new DomainEventNotification<GameProgressUpdatedDomainEvent>(new GameProgressUpdatedDomainEvent(_gameId)),
            CancellationToken.None
        );
    }
}