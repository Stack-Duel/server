using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Leaderboards;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using GetLeaderboardHandler = StackDuel.Application.Queries.Leaderboards.GetLeaderboard.GetLeaderboardHandler;
using GetLeaderboardQuery = StackDuel.Application.Queries.Leaderboards.GetLeaderboard.GetLeaderboardQuery;

namespace StackDuel.Application.Tests.Queries.Leaderboards.GetLeaderboard;

public class GetLeaderboardHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<ILeaderboardReadRepository> _leaderboardReadRepository = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private GetLeaderboardHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _leaderboardReadRepository = new Mock<ILeaderboardReadRepository>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _handler = new GetLeaderboardHandler(
            _gameReadRepository.Object,
            _leaderboardReadRepository.Object,
            _userReadRepository.Object
        );
    }

    private static UserDto MakeUser(Guid id, string username, string? imageUrl = null) =>
        new(id, $"auth0|{id}", username, imageUrl, null, false, null, DateTime.UtcNow, null, []);

    [Test]
    public async Task Handle_GameModeNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var query = new GetLeaderboardQuery("blitz", 300, new PaginationRequest { Page = 1, Size = 10 }, null);
        Result<PageResult<LeaderboardEntryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_Success_ComputesRankFromPageOffset()
    {
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var participant1 = new LeaderboardParticipant(Guid.NewGuid(), 100);
        var participant2 = new LeaderboardParticipant(Guid.NewGuid(), 90);
        var pagination = new PaginationRequest { Page = 2, Size = 5 };

        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _leaderboardReadRepository
            .Setup(x => x.GetRankingsAsync(gameMode.Id, 300, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<LeaderboardParticipant>
                {
                    Results = [participant1, participant2],
                    Total = 7,
                    Page = 2,
                    Size = 5,
                }
            );
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Dictionary<Guid, UserDto>
                {
                    [participant1.UserId] = MakeUser(participant1.UserId, "alice", "https://example.com/a.png"),
                    [participant2.UserId] = MakeUser(participant2.UserId, "bob"),
                }
            );

        var query = new GetLeaderboardQuery("blitz", 300, pagination, null);
        Result<PageResult<LeaderboardEntryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Total, Is.EqualTo(7));

        LeaderboardEntryDto entry1 = result.Value.Results[0];
        LeaderboardEntryDto entry2 = result.Value.Results[1];
        Assert.Multiple(() =>
        {
            Assert.That(entry1.Rank, Is.EqualTo(6));
            Assert.That(entry1.Username, Is.EqualTo("alice"));
            Assert.That(entry1.ImageUrl, Is.EqualTo("https://example.com/a.png"));
            Assert.That(entry1.HighScore, Is.EqualTo(100));
            Assert.That(entry2.Rank, Is.EqualTo(7));
            Assert.That(entry2.Username, Is.EqualTo("bob"));
        });
    }

    [Test]
    public async Task Handle_UserNotResolved_UsernameEmptyAndImageNull()
    {
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var participant = new LeaderboardParticipant(Guid.NewGuid(), 50);
        var pagination = new PaginationRequest { Page = 1, Size = 10 };

        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _leaderboardReadRepository
            .Setup(x => x.GetRankingsAsync(gameMode.Id, 300, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<LeaderboardParticipant>
                {
                    Results = [participant],
                    Total = 1,
                    Page = 1,
                    Size = 10,
                }
            );
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetLeaderboardQuery("blitz", 300, pagination, null);
        Result<PageResult<LeaderboardEntryDto>> result = await _handler.Handle(query, CancellationToken.None);

        LeaderboardEntryDto entry = result.Value.Results.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.Username, Is.EqualTo(string.Empty));
            Assert.That(entry.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task Handle_RequestedByUserIdMatchesParticipant_SetsIsCurrentUser()
    {
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var participant = new LeaderboardParticipant(Guid.NewGuid(), 50);
        var pagination = new PaginationRequest { Page = 1, Size = 10 };

        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _leaderboardReadRepository
            .Setup(x => x.GetRankingsAsync(gameMode.Id, 300, pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PageResult<LeaderboardParticipant>
                {
                    Results = [participant],
                    Total = 1,
                    Page = 1,
                    Size = 10,
                }
            );
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Dictionary<Guid, UserDto> { [participant.UserId] = MakeUser(participant.UserId, "alice") }
            );

        var query = new GetLeaderboardQuery("blitz", 300, pagination, participant.UserId);
        Result<PageResult<LeaderboardEntryDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Value.Results.Single().IsCurrentUser, Is.True);
    }
}