using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackDuel.Application.Announcements;
using StackDuel.Application.Commands.Games.CompleteExpiredGame;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Infrastructure.Messaging;

namespace StackDuel.Infrastructure.Tests.Messaging;

public class MessageSubscriptionHandlerTests
{
    private readonly Mock<IGameAnnouncer> _announcer = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly IServiceProvider _serviceProvider;

    private readonly Guid _gameId = Guid.NewGuid();
    private readonly DateTime _startedAt = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    public MessageSubscriptionHandlerTests()
    {
        ServiceCollection services = new();
        services.AddSingleton(_announcer.Object);
        services.AddSingleton(_mediator.Object);
        services.AddSingleton(_publisher.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static MessageSubscription SubscriptionFor<TMessage>()
        where TMessage : IMessage =>
        MessageSubscriptions.All.Single(subscription => subscription.MessageType == typeof(TMessage));

    private Task HandleAsync<TMessage>(TMessage message)
        where TMessage : class, IMessage =>
        SubscriptionFor<TMessage>().HandleAsync(_serviceProvider, message, CancellationToken.None);

    private void StubExpiryOutcome(CompleteExpiredGameOutcome outcome, DateTime? rescheduleFor) =>
        _mediator
            .Setup(x => x.Send(It.IsAny<CompleteExpiredGameCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CompleteExpiredGameResult(outcome, rescheduleFor)));

    private void VerifyNoReschedule() =>
        _publisher.Verify(
            x =>
                x.PublishScheduledAsync(
                    It.IsAny<GameTimeExpiredMessage>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );

    [Fact]
    public async Task GameCreatedMessage_IsRoutedToTheAnnouncer()
    {
        await HandleAsync(new GameCreatedMessage(_gameId));

        _announcer.Verify(x => x.AnnounceGameCreatedAsync(_gameId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GameTimeExpiredMessage_SendsTheCompleteExpiredGameCommand()
    {
        StubExpiryOutcome(CompleteExpiredGameOutcome.AlreadyFinalized_NoOp, null);

        await HandleAsync(new GameTimeExpiredMessage(_gameId, _startedAt, RescheduleCount: 0));

        _mediator.Verify(
            x =>
                x.Send(
                    It.Is<CompleteExpiredGameCommand>(c =>
                        c.GameId == _gameId && c.ExpectedStartedAt == _startedAt && c.RescheduleCount == 0
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GameTimeExpiredMessage_ArrivedEarly_RepublishesWithAnIncrementedRescheduleCount()
    {
        DateTime rescheduleFor = _startedAt.AddMinutes(5);
        StubExpiryOutcome(CompleteExpiredGameOutcome.NotYetExpired_Rescheduled, rescheduleFor);

        await HandleAsync(new GameTimeExpiredMessage(_gameId, _startedAt, RescheduleCount: 1));

        _publisher.Verify(
            x =>
                x.PublishScheduledAsync(
                    It.Is<GameTimeExpiredMessage>(m =>
                        m.GameId == _gameId && m.ExpectedStartedAt == _startedAt && m.RescheduleCount == 2
                    ),
                    rescheduleFor,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GameTimeExpiredMessage_Finalized_DoesNotReschedule()
    {
        StubExpiryOutcome(CompleteExpiredGameOutcome.AlreadyFinalized_NoOp, null);

        await HandleAsync(new GameTimeExpiredMessage(_gameId, _startedAt, RescheduleCount: 0));

        VerifyNoReschedule();
    }

    [Fact]
    public async Task GameTimeExpiredMessage_RescheduledWithoutATime_DoesNotReschedule()
    {
        StubExpiryOutcome(CompleteExpiredGameOutcome.NotYetExpired_Rescheduled, rescheduleFor: null);

        await HandleAsync(new GameTimeExpiredMessage(_gameId, _startedAt, RescheduleCount: 0));

        VerifyNoReschedule();
    }

    [Fact]
    public async Task GameTimeExpiredMessage_CommandFailed_DoesNotReschedule()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<CompleteExpiredGameCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompleteExpiredGameResult>.NotFound());

        await HandleAsync(new GameTimeExpiredMessage(_gameId, _startedAt, RescheduleCount: 0));

        VerifyNoReschedule();
    }
}