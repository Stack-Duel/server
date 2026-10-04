using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Queries.Users.GetUserGameStats;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Tests.Queries.Users.GetUserGameStats;

public class GetUserGameStatsHandlerTests
{
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private GetUserGameStatsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userReadRepository = new Mock<IUserReadRepository>();
        _gameReadRepository = new Mock<IGameReadRepository>();

        _handler = new GetUserGameStatsHandler(_userReadRepository.Object, _gameReadRepository.Object);

        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[]);
    }

    private static UserProfileDto CreateBaseProfile(Guid userId, bool isPrivate) =>
        new(userId, "alice", "bio", null, DateTime.UtcNow, isPrivate, false, null, null, null, null);

    [Test]
    public async Task Handle_ProfileNotFound_ReturnsNotFound()
    {
        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        Result<UserGameStatsDto> result = await _handler.Handle(
            new GetUserGameStatsQuery("missing", null),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_PrivateProfile_NotOwner_ReturnsNullStatsWithoutFetchingGames()
    {
        var profile = CreateBaseProfile(Guid.NewGuid(), isPrivate: true);
        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        Result<UserGameStatsDto> result = await _handler.Handle(
            new GetUserGameStatsQuery("alice", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.IsOwnProfile, Is.False);
            Assert.That(result.Value.GameModeStats, Is.Null);
        });
        _gameReadRepository.Verify(
            x => x.GetCompletedGamesForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_PublicProfile_ReturnsGameModeStats()
    {
        var userId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        var mode = new GameMode("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid());
        var game = new Game(mode.Id, Guid.NewGuid(), [Guid.NewGuid()], [userId, Guid.NewGuid()], 600);
        game.Start();
        game.RecordProblemSolved(userId);
        game.Complete();

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[mode]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[game]);

        Result<UserGameStatsDto> result = await _handler.Handle(
            new GetUserGameStatsQuery("alice", userId),
            CancellationToken.None
        );

        Assert.That(result.Value.GameModeStats!.Single().GamesPlayed, Is.EqualTo(1));
    }
}