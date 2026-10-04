using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using CloseLobbyCommand = StackDuel.Application.Commands.Games.CloseLobby.CloseLobbyCommand;
using CloseLobbyHandler = StackDuel.Application.Commands.Games.CloseLobby.CloseLobbyHandler;
using CloseLobbyValidator = StackDuel.Application.Commands.Games.CloseLobby.CloseLobbyValidator;

namespace StackDuel.Application.Tests.Commands.Games.CloseLobby;

public class CloseLobbyHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private CloseLobbyHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new CloseLobbyHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _domainEventDispatcher.Object,
            new CloseLobbyValidator()
        );
    }

    private static Game CreatePendingGame(out Guid hostId, out Guid otherId)
    {
        hostId = Guid.NewGuid();
        otherId = Guid.NewGuid();
        return new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId, otherId], 600);
    }

    [Test]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new CloseLobbyCommand(Guid.Empty, Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new CloseLobbyCommand(gameId, Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_RequesterIsNotHost_ReturnsForbidden()
    {
        var game = CreatePendingGame(out _, out var otherId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CloseLobbyCommand(game.Id, otherId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Forbidden));
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_GameNotPending_ReturnsInvalid()
    {
        var game = CreatePendingGame(out var hostId, out _);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CloseLobbyCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_HostClosesPendingLobby_CancelsGameAndDispatchesEvents()
    {
        var game = CreatePendingGame(out var hostId, out _);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CloseLobbyCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(game.Status, Is.EqualTo(StackDuel.Domain.Games.Enums.GameStatus.Cancelled));
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