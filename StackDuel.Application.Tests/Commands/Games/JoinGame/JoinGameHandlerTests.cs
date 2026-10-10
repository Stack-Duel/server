using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.SeedWork;
using JoinGameCommand = StackDuel.Application.Commands.Games.JoinGame.JoinGameCommand;
using JoinGameHandler = StackDuel.Application.Commands.Games.JoinGame.JoinGameHandler;
using JoinGameValidator = StackDuel.Application.Commands.Games.JoinGame.JoinGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.JoinGame;

public class JoinGameHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private UserContext _userContext = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private JoinGameHandler _handler = null!;

    public JoinGameHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _userContext = new UserContext();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new JoinGameHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _userContext,
            _domainEventDispatcher.Object,
            new JoinGameValidator()
        );
    }

    private static GameMode CreateGameMode(string key, int minPlayers, int maxPlayers) =>
        new(key, key, $"{key} description", isBuiltIn: true, minPlayers, maxPlayers, Guid.NewGuid());

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new JoinGameCommand(Guid.Empty, Guid.NewGuid());

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
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new JoinGameCommand(gameId, Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_GameNotPending_ReturnsInvalid()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_GameModeNotFound_ReturnsNotFound()
    {
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UnknownGameModeKey_ReturnsForbiddenWithoutCallingPermissionService()
    {
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        var gameMode = CreateGameMode("unknown_mode", 1, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);

        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        var gameMode = CreateGameMode("duel", 1, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userContext.Permissions = [];

        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_AlreadyJoined_ReturnsInvalid()
    {
        var gameModeId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [requesterId], 600);
        var gameMode = CreateGameMode("duel", 1, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        var command = new JoinGameCommand(game.Id, requesterId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_GameFull_ReturnsInvalid()
    {
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid(), Guid.NewGuid()], 600);
        var gameMode = CreateGameMode("duel", 1, 2);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        var command = new JoinGameCommand(game.Id, Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_Success_JoinsGameAndDispatchesEvents()
    {
        var gameModeId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        var gameMode = CreateGameMode("duel", 1, 4);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        var command = new JoinGameCommand(game.Id, requesterId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(game.Participants, p => p.UserId == requesterId);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}