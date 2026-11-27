using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Tests.Games;

public class GameExpiryCancellerTests
{
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly GameExpiryCanceller _canceller;

    public GameExpiryCancellerTests()
    {
        _canceller = new GameExpiryCanceller(
            _messagePublisher.Object,
            NullLogger<GameExpiryCanceller>.Instance
        );
    }

    private static Game CreatePendingGame() =>
        new(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], timeLimitInSeconds: 300);

    private static Game CreateCompletedGame(long? scheduledExpirySequenceNumber)
    {
        Game game = CreatePendingGame();
        game.Start();
        game.Complete();

        if (scheduledExpirySequenceNumber is long sequenceNumber)
            game.AttachScheduledExpiry(sequenceNumber);

        return game;
    }

    private void VerifyNoCancelAttempted() =>
        _messagePublisher.Verify(
            x => x.CancelScheduledAsync<GameTimeExpiredMessage>(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

    [Fact]
    public async Task CancelIfScheduledAsync_GameIsNotCompleted_DoesNothing()
    {
        Game game = CreatePendingGame();
        game.AttachScheduledExpiry(42);

        await _canceller.CancelIfScheduledAsync(game);

        VerifyNoCancelAttempted();
        Assert.Equal(42, game.ScheduledExpirySequenceNumber);
    }

    [Fact]
    public async Task CancelIfScheduledAsync_NoScheduledHandle_DoesNothing()
    {
        Game game = CreateCompletedGame(scheduledExpirySequenceNumber: null);

        await _canceller.CancelIfScheduledAsync(game);

        VerifyNoCancelAttempted();
    }

    [Fact]
    public async Task CancelIfScheduledAsync_CompletedWithHandle_CancelsAndClearsTheHandle()
    {
        Game game = CreateCompletedGame(scheduledExpirySequenceNumber: 99);

        await _canceller.CancelIfScheduledAsync(game);

        _messagePublisher.Verify(
            x => x.CancelScheduledAsync<GameTimeExpiredMessage>(99, It.IsAny<CancellationToken>()),
            Times.Once
        );
        Assert.Null(game.ScheduledExpirySequenceNumber);
    }

    [Fact]
    public async Task CancelIfScheduledAsync_TransportCannotCancel_StillClearsTheHandle()
    {
        Game game = CreateCompletedGame(scheduledExpirySequenceNumber: 99);
        _messagePublisher
            .Setup(x => x.CancelScheduledAsync<GameTimeExpiredMessage>(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException());

        await _canceller.CancelIfScheduledAsync(game);

        Assert.Null(game.ScheduledExpirySequenceNumber);
    }

    [Fact]
    public async Task CancelIfScheduledAsync_CancelFails_StillClearsTheHandle()
    {
        Game game = CreateCompletedGame(scheduledExpirySequenceNumber: 99);
        _messagePublisher
            .Setup(x => x.CancelScheduledAsync<GameTimeExpiredMessage>(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker is down"));

        await _canceller.CancelIfScheduledAsync(game);

        Assert.Null(game.ScheduledExpirySequenceNumber);
    }
}