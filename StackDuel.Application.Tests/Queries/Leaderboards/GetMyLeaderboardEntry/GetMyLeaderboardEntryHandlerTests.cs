using StackDuel.Application.Games;
using StackDuel.Application.Leaderboards;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;
using Moq;
using GetMyLeaderboardEntryHandler = StackDuel.Application.Queries.Leaderboards.GetMyLeaderboardEntry.GetMyLeaderboardEntryHandler;
using GetMyLeaderboardEntryQuery = StackDuel.Application.Queries.Leaderboards.GetMyLeaderboardEntry.GetMyLeaderboardEntryQuery;

namespace StackDuel.Application.Tests.Queries.Leaderboards.GetMyLeaderboardEntry;

public class GetMyLeaderboardEntryHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<ILeaderboardReadRepository> _leaderboardReadRepository = null!;
    private GetMyLeaderboardEntryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _leaderboardReadRepository = new Mock<ILeaderboardReadRepository>();
        _handler = new GetMyLeaderboardEntryHandler(_gameReadRepository.Object, _leaderboardReadRepository.Object);
    }

    [Test]
    public async Task Handle_GameModeNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var query = new GetMyLeaderboardEntryQuery("blitz", 300, Guid.NewGuid());
        Result<MyLeaderboardEntryDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_NoParticipantEntry_ReturnsNotFound()
    {
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var userId = Guid.NewGuid();

        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _leaderboardReadRepository
            .Setup(x => x.FindParticipantForUserAsync(gameMode.Id, 300, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaderboardParticipant?)null);

        var query = new GetMyLeaderboardEntryQuery("blitz", 300, userId);
        Result<MyLeaderboardEntryDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_ParticipantFound_ReturnsHighScore()
    {
        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var userId = Guid.NewGuid();
        var participant = new LeaderboardParticipant(userId, 42);

        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("blitz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _leaderboardReadRepository
            .Setup(x => x.FindParticipantForUserAsync(gameMode.Id, 300, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        var query = new GetMyLeaderboardEntryQuery("blitz", 300, userId);
        Result<MyLeaderboardEntryDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Value.HighScore, Is.EqualTo(42));
    }
}