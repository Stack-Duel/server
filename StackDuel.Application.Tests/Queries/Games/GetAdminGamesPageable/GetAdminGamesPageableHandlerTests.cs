using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using GetAdminGamesPageableHandler = StackDuel.Application.Queries.Games.GetAdminGamesPageable.GetAdminGamesPageableHandler;
using GetAdminGamesPageableQuery = StackDuel.Application.Queries.Games.GetAdminGamesPageable.GetAdminGamesPageableQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetAdminGamesPageable;

public class GetAdminGamesPageableHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetAdminGamesPageableHandler _handler = null!;

    public GetAdminGamesPageableHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _handler = new GetAdminGamesPageableHandler(_gameReadRepository.Object, _userReadRepository.Object);
    }

    private static UserDto MakeUser(Guid id, string username) =>
        new(id, $"auth0|{id}", username, null, null, false, null, DateTime.UtcNow, null, []);

    [Fact]
    public async Task Handle_PassesStatusAndPaginationToRepository()
    {
        var paginationRequest = new PaginationRequest { Page = 2, Size = 10 };
        _gameReadRepository
            .Setup(r =>
                r.GetAdminGamesPagedAsync(GameStatus.Completed, paginationRequest, It.IsAny<CancellationToken>())
            )
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
            .Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetAdminGamesPageableQuery(GameStatus.Completed, paginationRequest);
        Result<PageResult<AdminGameListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gameReadRepository.Verify(
            r => r.GetAdminGamesPagedAsync(GameStatus.Completed, paginationRequest, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ResolvesGameModeNamesAndParticipantUsernames()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 300);
        var gameMode = new GameMode("duel", "Duel", "desc", true, 2, 2, Guid.NewGuid());
        var paginationRequest = new PaginationRequest { Page = 1, Size = 20 };

        _gameReadRepository
            .Setup(r => r.GetAdminGamesPagedAsync(null, paginationRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [game],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _gameReadRepository
            .Setup(r => r.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto> { [userId] = MakeUser(userId, "riva") });

        var query = new GetAdminGamesPageableQuery(null, paginationRequest);
        Result<PageResult<AdminGameListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        AdminGameListItemDto item = result.Value.Results.Single();
        Assert.Equal(game.Id, item.GameId);
        Assert.Equal("duel", item.GameModeKey);
        Assert.Equal("Duel", item.GameModeName);
        Assert.Single(item.Participants);
        Assert.Equal("riva", item.Participants[0].Username);
    }

    [Fact]
    public async Task Handle_MissingGameMode_FallsBackToUnknown()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 300);
        var paginationRequest = new PaginationRequest { Page = 1, Size = 20 };

        _gameReadRepository
            .Setup(r => r.GetAdminGamesPagedAsync(null, paginationRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<Game>
                {
                    Results = [game],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _gameReadRepository
            .Setup(r => r.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);
        _userReadRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetAdminGamesPageableQuery(null, paginationRequest);
        Result<PageResult<AdminGameListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        AdminGameListItemDto item = result.Value.Results.Single();
        Assert.Equal("unknown", item.GameModeKey);
        Assert.Equal("Unknown", item.GameModeName);
    }
}