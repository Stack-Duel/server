using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Tracks.Entities;
using GetMyActiveGamesHandler = StackDuel.Application.Queries.Games.GetMyActiveGames.GetMyActiveGamesHandler;
using GetMyActiveGamesQuery = StackDuel.Application.Queries.Games.GetMyActiveGames.GetMyActiveGamesQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetMyActiveGames;

public class GetMyActiveGamesHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetMyActiveGamesHandler _handler = null!;

    private static UserDto MakeUser(Guid id, string username) =>
        new(id, $"auth0|{id}", username, null, null, false, null, DateTime.UtcNow, null, []);

    public GetMyActiveGamesHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _trackReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _userReadRepository = new Mock<IUserReadRepository>();
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());
        _handler = new GetMyActiveGamesHandler(
            _gameReadRepository.Object,
            _trackReadRepository.Object,
            _userReadRepository.Object
        );
    }

    [Fact]
    public async Task Handle_NoActiveGames_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.GetActiveGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetMyActiveGamesQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_GameWithMissingGameMode_IsExcludedFromResults()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 300);

        _gameReadRepository
            .Setup(x => x.GetActiveGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([game]);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var result = await _handler.Handle(new GetMyActiveGamesQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_Success_MapsFieldsAndSetsIsHostForLowestSeat()
    {
        var hostId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var track = new Track("python", "Python");
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [track.Id], [hostId, otherId], 300);
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());

        _gameReadRepository
            .Setup(x => x.GetActiveGamesForUserAsync(hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([game]);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _trackReadRepository
            .Setup(x =>
                x.FindByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(track.Id)), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([track]);
        _userReadRepository
            .Setup(x =>
                x.FindByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(hostId)), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new Dictionary<Guid, UserDto> { [hostId] = MakeUser(hostId, "hostname") });

        var result = await _handler.Handle(new GetMyActiveGamesQuery(hostId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        MyActiveGameDto dto = result.Value.Single();
        Assert.Equal(game.Id, dto.GameId);
        Assert.Equal("blitz", dto.GameModeKey);
        Assert.Equal("Blitz", dto.GameModeName);
        Assert.Equal(GameStatus.Pending, dto.Status);
        Assert.Equal(300, dto.TimeLimitInSeconds);
        Assert.Equal(2, dto.ParticipantCount);
        Assert.Equal(4, dto.MaxPlayers);
        Assert.True(dto.IsHost);
        Assert.Equal("hostname", dto.HostUsername);
        Assert.Equivalent(new[] { "Python" }, dto.TechStacks, strict: true);
    }

    [Fact]
    public async Task Handle_Success_NonHostParticipant_IsHostIsFalse()
    {
        var hostId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId, otherId], 300);
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());

        _gameReadRepository
            .Setup(x => x.GetActiveGamesForUserAsync(otherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([game]);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);

        var result = await _handler.Handle(new GetMyActiveGamesQuery(otherId), CancellationToken.None);

        Assert.False(result.Value.Single().IsHost);
    }
}