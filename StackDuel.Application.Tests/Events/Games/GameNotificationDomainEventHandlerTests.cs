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

    private GameCompletedDomainEventHandler CompletedHandler() =>
        new(_notifications.Object, NullLogger<GameCompletedDomainEventHandler>.Instance);

    private GameLobbyUpdatedDomainEventHandler LobbyHandler() =>
        new(_notifications.Object, NullLogger<GameLobbyUpdatedDomainEventHandler>.Instance);

    private GameProgressUpdatedDomainEventHandler ProgressHandler() =>
        new(_notifications.Object, NullLogger<GameProgressUpdatedDomainEventHandler>.Instance);

    private static DomainEventNotification<GameCompletedDomainEvent> Completed(Guid gameId, DateTime endedAt) =>
        new(new GameCompletedDomainEvent(gameId, GameStatus.Completed, endedAt));

    private static DomainEventNotification<GameLobbyUpdatedDomainEvent> LobbyUpdated(Guid gameId) =>
        new(new GameLobbyUpdatedDomainEvent(gameId));

    private static DomainEventNotification<GameProgressUpdatedDomainEvent> ProgressUpdated(Guid gameId) =>
        new(new GameProgressUpdatedDomainEvent(gameId));

    [Fact]
    public async Task GameCompletedHandler_PushesTheCompletionToWhoeverIsWatching()
    {
        DateTime endedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await CompletedHandler().Handle(Completed(_gameId, endedAt), CancellationToken.None);

        _notifications.Verify(x => x.NotifyGameCompletedAsync(_gameId, GameStatus.Completed, endedAt), Times.Once);
    }

    [Fact]
    public async Task GameCompletedHandler_SwallowsAPushFailure()
    {
        _notifications
            .Setup(x => x.NotifyGameCompletedAsync(It.IsAny<Guid>(), It.IsAny<GameStatus>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        Exception? exception = await Record.ExceptionAsync(
            () => CompletedHandler().Handle(Completed(_gameId, DateTime.UtcNow), CancellationToken.None)
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task GameLobbyUpdatedHandler_PushesTheLobbyUpdate()
    {
        await LobbyHandler().Handle(LobbyUpdated(_gameId), CancellationToken.None);

        _notifications.Verify(x => x.NotifyGameLobbyUpdatedAsync(_gameId), Times.Once);
    }

    [Fact]
    public async Task GameLobbyUpdatedHandler_SwallowsAPushFailure()
    {
        _notifications
            .Setup(x => x.NotifyGameLobbyUpdatedAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        Exception? exception = await Record.ExceptionAsync(
            () => LobbyHandler().Handle(LobbyUpdated(_gameId), CancellationToken.None)
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task GameProgressUpdatedHandler_PushesTheProgressUpdate()
    {
        await ProgressHandler().Handle(ProgressUpdated(_gameId), CancellationToken.None);

        _notifications.Verify(x => x.NotifyGameProgressUpdatedAsync(_gameId), Times.Once);
    }

    [Fact]
    public async Task GameProgressUpdatedHandler_SwallowsAPushFailure()
    {
        _notifications
            .Setup(x => x.NotifyGameProgressUpdatedAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        Exception? exception = await Record.ExceptionAsync(
            () => ProgressHandler().Handle(ProgressUpdated(_gameId), CancellationToken.None)
        );

        Assert.Null(exception);
    }
}