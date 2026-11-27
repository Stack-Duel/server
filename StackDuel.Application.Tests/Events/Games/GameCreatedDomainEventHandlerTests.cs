using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Events.Games;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Games.Events;

namespace StackDuel.Application.Tests.Events.Games;

public class GameCreatedDomainEventHandlerTests
{
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly Guid _gameId = Guid.NewGuid();

    private GameCreatedDomainEventHandler Handler() =>
        new(_messagePublisher.Object, NullLogger<GameCreatedDomainEventHandler>.Instance);

    private DomainEventNotification<GameCreatedDomainEvent> Created() => new(new GameCreatedDomainEvent(_gameId));

    [Fact]
    public async Task Handle_PublishesAGameCreatedMessageForTheGame()
    {
        await Handler().Handle(Created(), CancellationToken.None);

        _messagePublisher.Verify(
            x => x.PublishAsync(It.Is<GameCreatedMessage>(m => m.GameId == _gameId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_PublishFails_DoesNotFaultTheCommandThatCreatedTheGame()
    {
        _messagePublisher
            .Setup(x => x.PublishAsync(It.IsAny<GameCreatedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker is down"));

        await Handler().Handle(Created(), CancellationToken.None);
    }
}