using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;
using Moq;
using GetDuelHeadToHeadRecordHandler = StackDuel.Application.Queries.Games.GetDuelHeadToHeadRecord.GetDuelHeadToHeadRecordHandler;
using GetDuelHeadToHeadRecordQuery = StackDuel.Application.Queries.Games.GetDuelHeadToHeadRecord.GetDuelHeadToHeadRecordQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetDuelHeadToHeadRecord;

public class GetDuelHeadToHeadRecordHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private GetDuelHeadToHeadRecordHandler _handler = null!;
    private readonly GameMode _duelMode = new("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid());

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _handler = new GetDuelHeadToHeadRecordHandler(_gameReadRepository.Object);

        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_duelMode);
    }

    private static Game CreateCompletedGame(Guid gameModeId, params (Guid UserId, int Score)[] participants)
    {
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], participants.Select(p => p.UserId), 600);
        game.Start();
        foreach (var (userId, score) in participants)
            for (int i = 0; i < score; i++)
                game.RecordProblemSolved(userId);
        game.Complete();
        return game;
    }

    [Test]
    public async Task Handle_DuelModeNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        Result<DuelHeadToHeadRecordDto> result = await _handler.Handle(
            new GetDuelHeadToHeadRecordQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_TalliesWinsLossesAndDrawsAgainstThatSpecificOpponentOnly()
    {
        var userId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var win = CreateCompletedGame(_duelMode.Id, (userId, 5), (opponentId, 2));
        var loss = CreateCompletedGame(_duelMode.Id, (userId, 1), (opponentId, 4));
        var draw = CreateCompletedGame(_duelMode.Id, (userId, 3), (opponentId, 3));
        var againstSomeoneElse = CreateCompletedGame(_duelMode.Id, (userId, 5), (strangerId, 0));
        var differentMode = CreateCompletedGame(Guid.NewGuid(), (userId, 5), (opponentId, 0));

        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[win, loss, draw, againstSomeoneElse, differentMode]);

        Result<DuelHeadToHeadRecordDto> result = await _handler.Handle(
            new GetDuelHeadToHeadRecordQuery(userId, opponentId),
            CancellationToken.None
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Wins, Is.EqualTo(1));
            Assert.That(result.Value.Losses, Is.EqualTo(1));
            Assert.That(result.Value.Draws, Is.EqualTo(1));
            Assert.That(result.Value.GamesPlayed, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Handle_NoSharedGames_ReturnsAllZeroes()
    {
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[]);

        Result<DuelHeadToHeadRecordDto> result = await _handler.Handle(
            new GetDuelHeadToHeadRecordQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Value.GamesPlayed, Is.EqualTo(0));
    }
}