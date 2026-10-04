using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CompleteExpiredGameCommand = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameCommand;
using CompleteExpiredGameHandler = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameHandler;
using CompleteExpiredGameOutcome = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameOutcome;
using CompleteExpiredGameResult = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameResult;
using CompleteExpiredGameValidator = StackDuel.Application.Commands.Games.CompleteExpiredGame.CompleteExpiredGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.CompleteExpiredGame;

public class CompleteExpiredGameHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private CompleteExpiredGameHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new CompleteExpiredGameHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            new CompleteExpiredGameValidator(),
            _domainEventDispatcher.Object,
            NullLogger<CompleteExpiredGameHandler>.Instance
        );
    }

    [Test]
    public async Task Handle_InvalidCommand_ReturnsInvalid()
    {
        var command = new CompleteExpiredGameCommand(Guid.NewGuid(), DateTime.UtcNow, -1);

        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_GameNotFound_ReturnsStaleDropped()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new CompleteExpiredGameCommand(gameId, DateTime.UtcNow, 0);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Outcome, Is.EqualTo(CompleteExpiredGameOutcome.Stale_Dropped));
    }

    [Test]
    public async Task Handle_GameNotRunning_ReturnsAlreadyFinalizedNoOp()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteExpiredGameCommand(game.Id, DateTime.UtcNow, 0);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Value.Outcome, Is.EqualTo(CompleteExpiredGameOutcome.AlreadyFinalized_NoOp));
    }

    [Test]
    public async Task Handle_StartedAtDoesNotMatchExpected_ReturnsStaleDropped()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteExpiredGameCommand(game.Id, game.StartedAt!.Value.AddMinutes(-5), 0);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Value.Outcome, Is.EqualTo(CompleteExpiredGameOutcome.Stale_Dropped));
    }

    [Test]
    public async Task Handle_NotYetExpired_BelowRescheduleLimit_ReturnsRescheduled()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteExpiredGameCommand(game.Id, game.StartedAt!.Value, 0);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Outcome, Is.EqualTo(CompleteExpiredGameOutcome.NotYetExpired_Rescheduled));
            Assert.That(result.Value.RescheduleForUtc, Is.Not.Null);
            Assert.That(game.Status, Is.EqualTo(StackDuel.Domain.Games.Enums.GameStatus.Running));
        });
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_NotYetExpired_RescheduleLimitReached_CompletesGameAnyway()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteExpiredGameCommand(game.Id, game.StartedAt!.Value, 3);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Value.Outcome,
                Is.EqualTo(CompleteExpiredGameOutcome.NotYetExpired_RescheduleLimitReached_CompletedAnyway)
            );
            Assert.That(game.Status, Is.EqualTo(StackDuel.Domain.Games.Enums.GameStatus.Completed));
        });
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<StackDuel.Domain.SeedWork.IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_NotYetExpired_ImplausibleDelay_ReturnsDropped()
    {
        var game = new Game(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()],
            [Guid.NewGuid()],
            (int)TimeSpan.FromHours(8).TotalSeconds
        );
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteExpiredGameCommand(game.Id, game.StartedAt!.Value, 0);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(
            result.Value.Outcome,
            Is.EqualTo(CompleteExpiredGameOutcome.NotYetExpired_ImplausibleDelay_Dropped)
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_GameHasExpired_CompletesGameAndReturnsCompleted()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 1);
        game.Start();
        typeof(Game).GetProperty(nameof(Game.StartedAt))!.SetValue(game, DateTime.UtcNow.AddSeconds(-2));
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteExpiredGameCommand(game.Id, game.StartedAt!.Value, 0);
        Result<CompleteExpiredGameResult> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Outcome, Is.EqualTo(CompleteExpiredGameOutcome.Completed));
            Assert.That(game.Status, Is.EqualTo(StackDuel.Domain.Games.Enums.GameStatus.Completed));
        });
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<StackDuel.Domain.SeedWork.IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}