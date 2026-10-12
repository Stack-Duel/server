using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Tracks.Entities;
using GetOpenGamesHandler = StackDuel.Application.Queries.Games.GetOpenGames.GetOpenGamesHandler;
using GetOpenGamesQuery = StackDuel.Application.Queries.Games.GetOpenGames.GetOpenGamesQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetOpenGames;

public class GetOpenGamesHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private GetOpenGamesHandler _handler = null!;

    public GetOpenGamesHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _trackReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _handler = new GetOpenGamesHandler(
            _gameReadRepository.Object,
            _userReadRepository.Object,
            _trackReadRepository.Object
        );
    }

    private static UserDto MakeUser(Guid id, string username) =>
        new(id, $"auth0|{id}", username, null, null, false, null, DateTime.UtcNow, null, []);

    [Fact]
    public async Task Handle_PassesGameModeKeyAndPaginationToRepository()
    {
        var pagination = new PaginationRequest { Page = 2, Size = 10 };
        _gameReadRepository
            .Setup(x => x.GetPendingGamesAsync("blitz", pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [],
                    Total = 0,
                    Page = 2,
                    Size = 10,
                }
            );
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetOpenGamesQuery("blitz", pagination, null);
        Result<PageResult<GameLobbySummaryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Total);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(10, result.Value.Size);
        _gameReadRepository.Verify(
            x => x.GetPendingGamesAsync("blitz", pagination, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_GameWithMissingGameMode_IsExcludedFromResults()
    {
        var hostId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId], 300);
        var pagination = new PaginationRequest { Page = 1, Size = 10 };

        _gameReadRepository
            .Setup(x => x.GetPendingGamesAsync(null, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [game],
                    Total = 1,
                    Page = 1,
                    Size = 10,
                }
            );
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetOpenGamesQuery(null, pagination, null);
        Result<PageResult<GameLobbySummaryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result.Value.Results);
    }

    [Fact]
    public async Task Handle_Success_MapsSummaryWithHostAndParticipantFlags()
    {
        var hostId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var track = new Track("python", "Python");
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [track.Id], [hostId, otherId], 300);
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var pagination = new PaginationRequest { Page = 1, Size = 10 };

        _gameReadRepository
            .Setup(x => x.GetPendingGamesAsync(null, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [game],
                    Total = 1,
                    Page = 1,
                    Size = 10,
                }
            );
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(x =>
                x.FindByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(hostId)), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new Dictionary<Guid, UserDto> { [hostId] = MakeUser(hostId, "hostname") });
        _trackReadRepository
            .Setup(x =>
                x.FindByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(track.Id)), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([track]);

        var query = new GetOpenGamesQuery(null, pagination, hostId);
        Result<PageResult<GameLobbySummaryDto>> result = await _handler.Handle(query, CancellationToken.None);

        GameLobbySummaryDto summary = result.Value.Results.Single();
        Assert.Equal(game.Id, summary.GameId);
        Assert.Equal("blitz", summary.GameModeKey);
        Assert.Equal("Blitz", summary.GameModeName);
        Assert.Equal(1, summary.MinPlayers);
        Assert.Equal(4, summary.MaxPlayers);
        Assert.Equal(2, summary.ParticipantCount);
        Assert.Equal("hostname", summary.HostUsername);
        Assert.True(summary.IsHost);
        Assert.True(summary.IsParticipant);
        Assert.Equivalent(new[] { "Python" }, summary.TechStacks, strict: true);
    }

    [Fact]
    public async Task Handle_RequestedByUserIdNull_IsHostAndIsParticipantAreFalse()
    {
        var hostId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId], 300);
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var pagination = new PaginationRequest { Page = 1, Size = 10 };

        _gameReadRepository
            .Setup(x => x.GetPendingGamesAsync(null, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [game],
                    Total = 1,
                    Page = 1,
                    Size = 10,
                }
            );
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto> { [hostId] = MakeUser(hostId, "hostname") });

        var query = new GetOpenGamesQuery(null, pagination, null);
        Result<PageResult<GameLobbySummaryDto>> result = await _handler.Handle(query, CancellationToken.None);

        GameLobbySummaryDto summary = result.Value.Results.Single();
        Assert.False(summary.IsHost);
        Assert.False(summary.IsParticipant);
    }

    [Fact]
    public async Task Handle_HostNotResolvedInUserMap_HostUsernameIsEmpty()
    {
        var hostId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [hostId], 300);
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var pagination = new PaginationRequest { Page = 1, Size = 10 };

        _gameReadRepository
            .Setup(x => x.GetPendingGamesAsync(null, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [game],
                    Total = 1,
                    Page = 1,
                    Size = 10,
                }
            );
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetOpenGamesQuery(null, pagination, null);
        Result<PageResult<GameLobbySummaryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(string.Empty, result.Value.Results.Single().HostUsername);
    }
}