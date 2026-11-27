using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Events.Games;
using StackDuel.Application.Games;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Events;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Tests.Events.Games;

public class GameStartedDomainEventHandlerTests
{
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly GameStartedDomainEventHandler _handler;

    private readonly Guid _gameId = Guid.NewGuid();
    private readonly DateTime _startedAt = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    public GameStartedDomainEventHandlerTests()
    {
        _handler = new GameStartedDomainEventHandler(
            _messagePublisher.Object,
            _gameReadRepository.Object,
            _unitOfWork.Object,
            NullLogger<GameStartedDomainEventHandler>.Instance
        );
    }

    private DomainEventNotification<GameStartedDomainEvent> Started(int timeLimitInSeconds = 600) =>
        new(new GameStartedDomainEvent(_gameId, _startedAt, timeLimitInSeconds));

    private void StubPublishReturns(long sequenceNumber) =>
        _messagePublisher
            .Setup(x =>
                x.PublishScheduledAsync(
                    It.IsAny<GameTimeExpiredMessage>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(sequenceNumber);

    private static Game CreateStartedGame()
    {
        Game game = new(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], timeLimitInSeconds: 600);
        game.Start();
        return game;
    }

    private void VerifyNoSave() =>
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Handle_SchedulesExpiryForTheEndOfTheTimeLimit()
    {
        StubPublishReturns(0);

        await _handler.Handle(Started(timeLimitInSeconds: 600), CancellationToken.None);

        _messagePublisher.Verify(
            x =>
                x.PublishScheduledAsync(
                    It.Is<GameTimeExpiredMessage>(m =>
                        m.GameId == _gameId && m.ExpectedStartedAt == _startedAt && m.RescheduleCount == 0
                    ),
                    new DateTimeOffset(_startedAt.AddSeconds(600)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_TransportReturnsNoUsableHandle_DoesNotTouchTheGame()
    {
        StubPublishReturns(0);

        await _handler.Handle(Started(), CancellationToken.None);

        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        VerifyNoSave();
    }

    [Fact]
    public async Task Handle_UsableHandle_PersistsItOnTheGame()
    {
        StubPublishReturns(1234);
        Game game = CreateStartedGame();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(_gameId, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        await _handler.Handle(Started(), CancellationToken.None);

        Assert.Equal(1234, game.ScheduledExpirySequenceNumber);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GameNoLongerExists_DoesNotSave()
    {
        StubPublishReturns(1234);
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(_gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        await _handler.Handle(Started(), CancellationToken.None);

        VerifyNoSave();
    }

    [Fact]
    public async Task Handle_SchedulingFails_DoesNotFaultTheGameStartOrTouchTheGame()
    {
        _messagePublisher
            .Setup(x =>
                x.PublishScheduledAsync(
                    It.IsAny<GameTimeExpiredMessage>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("broker is down"));

        await _handler.Handle(Started(), CancellationToken.None);

        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        VerifyNoSave();
    }
}