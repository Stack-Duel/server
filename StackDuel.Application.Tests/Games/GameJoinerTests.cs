using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Tests.Games;

public class GameJoinerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UserContext _userContext = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private GameJoiner _joiner = null!;

    public GameJoinerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _userContext = new UserContext();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _joiner = new GameJoiner(
            _gameReadRepository.Object,
            _unitOfWork.Object,
            _userContext,
            _domainEventDispatcher.Object
        );
    }

    private static GameMode CreateGameMode(string key, int minPlayers, int maxPlayers) =>
        new(key, key, $"{key} description", isBuiltIn: true, minPlayers, maxPlayers, Guid.NewGuid());

    [Fact]
    public async Task JoinAsync_GameNotFound_ReturnsNotFound()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        Result result = await _joiner.JoinAsync(gameId, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task JoinAsync_GameNotPending_ReturnsInvalid()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        Result result = await _joiner.JoinAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task JoinAsync_GameModeNotFound_ReturnsNotFound()
    {
        var gameModeId = Guid.NewGuid();
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(gameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        Result result = await _joiner.JoinAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task JoinAsync_UnknownGameModeKey_ReturnsForbiddenWithoutCallingPermissionService()
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

        Result result = await _joiner.JoinAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task JoinAsync_NoPermission_ReturnsForbidden()
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

        Result result = await _joiner.JoinAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task JoinAsync_AlreadyJoined_ReturnsInvalid()
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

        Result result = await _joiner.JoinAsync(game.Id, requesterId, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task JoinAsync_GameFull_ReturnsInvalid()
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

        Result result = await _joiner.JoinAsync(game.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task JoinAsync_Success_JoinsGameAndDispatchesEvents()
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

        Result result = await _joiner.JoinAsync(game.Id, requesterId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(game.Participants, p => p.UserId == requesterId);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}