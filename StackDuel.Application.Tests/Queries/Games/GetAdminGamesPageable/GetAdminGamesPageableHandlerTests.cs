using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using Ardalis.Result;
using Moq;
using GetAdminGamesPageableHandler = StackDuel.Application.Queries.Games.GetAdminGamesPageable.GetAdminGamesPageableHandler;
using GetAdminGamesPageableQuery = StackDuel.Application.Queries.Games.GetAdminGamesPageable.GetAdminGamesPageableQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetAdminGamesPageable;

public class GetAdminGamesPageableHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetAdminGamesPageableHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _handler = new GetAdminGamesPageableHandler(_gameReadRepository.Object, _userReadRepository.Object);
    }

    private static UserDto MakeUser(Guid id, string username) =>
        new(id, $"auth0|{id}", username, null, null, false, null, DateTime.UtcNow, null, []);

    [Test]
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

        Assert.That(result.IsSuccess, Is.True);
        _gameReadRepository.Verify(
            r => r.GetAdminGamesPagedAsync(GameStatus.Completed, paginationRequest, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
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

        Assert.That(result.IsSuccess, Is.True);
        AdminGameListItemDto item = result.Value.Results.Single();
        Assert.Multiple(() =>
        {
            Assert.That(item.GameId, Is.EqualTo(game.Id));
            Assert.That(item.GameModeKey, Is.EqualTo("duel"));
            Assert.That(item.GameModeName, Is.EqualTo("Duel"));
            Assert.That(item.Participants, Has.Count.EqualTo(1));
            Assert.That(item.Participants[0].Username, Is.EqualTo("riva"));
        });
    }

    [Test]
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
        Assert.That(item.GameModeKey, Is.EqualTo("unknown"));
        Assert.That(item.GameModeName, Is.EqualTo("Unknown"));
    }
}