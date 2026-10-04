using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using GetAdminGameDetailHandler = StackDuel.Application.Queries.Games.GetAdminGameDetail.GetAdminGameDetailHandler;
using GetAdminGameDetailQuery = StackDuel.Application.Queries.Games.GetAdminGameDetail.GetAdminGameDetailQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetAdminGameDetail;

public class GetAdminGameDetailHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private GetAdminGameDetailHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _trackReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _handler = new GetAdminGameDetailHandler(
            _gameReadRepository.Object,
            _userReadRepository.Object,
            _trackReadRepository.Object
        );
    }

    private static UserDto MakeUser(Guid id, string username) =>
        new(id, $"auth0|{id}", username, null, null, false, null, DateTime.UtcNow, null, []);

    [Test]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(r => r.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var result = await _handler.Handle(new GetAdminGameDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_GameModeMissing_ReturnsError()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 300);

        _gameReadRepository.Setup(r => r.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(r => r.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var result = await _handler.Handle(new GetAdminGameDetailQuery(game.Id), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Error));
    }

    [Test]
    public async Task Handle_RequestedByNonParticipant_StillSucceeds()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);
        var gameMode = new GameMode("duel", "Duel", "desc", true, 2, 2, Guid.NewGuid());

        _gameReadRepository.Setup(r => r.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(r => r.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto> { [participantId] = MakeUser(participantId, "alice") });

        var result = await _handler.Handle(new GetAdminGameDetailQuery(game.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Participants.Single().Username, Is.EqualTo("alice"));
    }

    [Test]
    public async Task Handle_ExpiredRunningGame_DoesNotCompleteIt()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 1);
        game.Start();
        var gameMode = new GameMode("duel", "Duel", "desc", true, 2, 2, Guid.NewGuid());

        _gameReadRepository.Setup(r => r.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(r => r.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var result = await _handler.Handle(new GetAdminGameDetailQuery(game.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(game.Status, Is.EqualTo(GameStatus.Running), "admin read must not mutate game state");
        Assert.That(result.Value.Status, Is.EqualTo(GameStatus.Running));
    }
}