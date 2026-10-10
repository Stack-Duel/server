using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using StartGameCommand = StackDuel.Application.Commands.Games.StartGame.StartGameCommand;
using StartGameHandler = StackDuel.Application.Commands.Games.StartGame.StartGameHandler;
using StartGameValidator = StackDuel.Application.Commands.Games.StartGame.StartGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.StartGame;

public class StartGameHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private Mock<IGameProblemSequencer> _gameProblemSequencer = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private StartGameHandler _handler = null!;

    public StartGameHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();
        _gameProblemSequencer = new Mock<IGameProblemSequencer>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new StartGameHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _strategyResolver.Object,
            _gameProblemSequencer.Object,
            _domainEventDispatcher.Object,
            new StartGameValidator()
        );
    }

    private static GameMode CreateGameMode(string key, int minPlayers, int maxPlayers) =>
        new(key, key, $"{key} description", isBuiltIn: true, minPlayers, maxPlayers, Guid.NewGuid());

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new StartGameCommand(Guid.Empty, Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new StartGameCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NotHost_ReturnsForbidden()
    {
        var hostId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId, Guid.NewGuid()], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new StartGameCommand(game.Id, Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_GameNotPending_ReturnsInvalid()
    {
        var hostId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_GameModeNotFound_ReturnsNotFoundAndDoesNotStartGame()
    {
        var hostId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(GameStatus.Pending, game.Status);
    }

    [Fact]
    public async Task Handle_NotEnoughPlayers_ReturnsInvalidAndDoesNotStartGame()
    {
        var hostId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);
        var gameMode = CreateGameMode("duel", 2, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(GameStatus.Pending, game.Status);
    }

    [Fact]
    public async Task Handle_StrategyNotResolved_ReturnsNotFound()
    {
        var hostId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);
        var gameMode = CreateGameMode("duel", 1, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _strategyResolver.Setup(x => x.Resolve("duel")).Returns((IProblemSelectionStrategy)null!);

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_NoInitialProblemSelected_ReturnsInvalid()
    {
        var hostId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);
        var gameMode = CreateGameMode("duel", 1, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _strategyResolver.Setup(x => x.Resolve("duel")).Returns(_strategy.Object);
        _gameProblemSequencer
            .Setup(x => x.GetOrGenerateProblemAsync(game, 0, "duel", _strategy.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Success_StartsGameAndInitializesProblemSessions()
    {
        var hostId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId, otherId], 600);
        var gameMode = CreateGameMode("duel", 1, 4);
        var initialProblemId = Guid.NewGuid();

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _strategyResolver.Setup(x => x.Resolve("duel")).Returns(_strategy.Object);
        _gameProblemSequencer
            .Setup(x => x.GetOrGenerateProblemAsync(game, 0, "duel", _strategy.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialProblemId);

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameStatus.Running, game.Status);
        Assert.True(game.Participants.All(p => p.ProblemSession!.CurrentProblemId == initialProblemId));
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Theory]
    [InlineData("duel")]
    [InlineData("ffa")]
    public async Task Handle_MultiplayerMode_DelaysStartedAtForASharedCountdown(string gameModeKey)
    {
        var hostId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);
        var gameMode = CreateGameMode(gameModeKey, 1, 4);
        DateTime beforeStart = DateTime.UtcNow;

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _strategyResolver.Setup(x => x.Resolve(gameModeKey)).Returns(_strategy.Object);
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(game, 0, gameModeKey, _strategy.Object, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Guid.NewGuid());

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(
            game.StartedAt > beforeStart.AddSeconds(1),
            "multiplayer modes should get a real countdown window before StartedAt"
        );
    }

    [Fact]
    public async Task Handle_SoloRush_DoesNotDelayStartedAt()
    {
        var hostId = Guid.NewGuid();
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [hostId], 600);
        var gameMode = CreateGameMode("solo_rush", 1, 1);
        DateTime beforeStart = DateTime.UtcNow;

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _strategyResolver.Setup(x => x.Resolve("solo_rush")).Returns(_strategy.Object);
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(game, 0, "solo_rush", _strategy.Object, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Guid.NewGuid());

        var command = new StartGameCommand(game.Id, hostId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(game.StartedAt < beforeStart.AddSeconds(1));
    }
}