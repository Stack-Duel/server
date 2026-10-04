using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Tracks.Entities;
using GetGameStateHandler = StackDuel.Application.Queries.Games.GetGameState.GetGameStateHandler;
using GetGameStateQuery = StackDuel.Application.Queries.Games.GetGameState.GetGameStateQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameState;

public class GetGameStateHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IGameExpiryCanceller> _gameExpiryCanceller = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private GetGameStateHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _gameExpiryCanceller = new Mock<IGameExpiryCanceller>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _trackReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new GetGameStateHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _gameExpiryCanceller.Object,
            _domainEventDispatcher.Object,
            _userReadRepository.Object,
            _trackReadRepository.Object
        );
    }

    private static UserDto MakeUser(Guid id, string username, string? imageUrl = null) =>
        new(id, $"auth0|{id}", username, imageUrl, null, false, null, DateTime.UtcNow, null, []);

    [Test]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var query = new GetGameStateQuery(Guid.NewGuid(), Guid.NewGuid());
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_RequestedByNonParticipant_ReturnsForbidden()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameStateQuery(game.Id, Guid.NewGuid());
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Forbidden));
    }

    [Test]
    public async Task Handle_GameModeMissing_ReturnsError()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var query = new GetGameStateQuery(game.Id, participantId);
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Error));
    }

    [Test]
    public async Task Handle_EveryoneStopped_SelfHealsCompletionAndCancelsScheduledExpiry()
    {
        var participantAId = Guid.NewGuid();
        var participantBId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantAId, participantBId], 300);
        game.Start();

        foreach (GameParticipant participant in game.Participants)
            participant.Forfeit();

        Assert.That(
            game.Status,
            Is.EqualTo(GameStatus.Running),
            "sanity: race simulated by forfeiting without Game.Forfeit's own self-heal"
        );

        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetGameStateQuery(game.Id, participantAId);
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(game.Status, Is.EqualTo(GameStatus.Completed));
        Assert.That(result.Value.Status, Is.EqualTo(GameStatus.Completed));

        _gameExpiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_RunningGame_DoesNotPersistWhenNothingChanged()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);
        game.Start();

        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var query = new GetGameStateQuery(game.Id, participantId);
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_Success_MapsParticipantsAndFallsBackForMissingUsers()
    {
        var knownUserId = Guid.NewGuid();
        var unknownUserId = Guid.NewGuid();
        var track = new Track("python", "Python");
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [track.Id], [knownUserId, unknownUserId], 600);
        game.Start();

        var initialProblemId = Guid.NewGuid();
        GameParticipant known = game.Participants.Single(p => p.UserId == knownUserId);
        known.InitializeProblemSession(initialProblemId);
        known.IncrementScore();

        var gameMode = new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid());
        var knownUserDto = MakeUser(knownUserId, "alice", "https://example.com/a.png");

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto> { [knownUserId] = knownUserDto });
        _trackReadRepository
            .Setup(x =>
                x.FindByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(track.Id)), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([track]);

        var query = new GetGameStateQuery(game.Id, knownUserId);
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        GameStateDto dto = result.Value;

        Assert.Multiple(() =>
        {
            Assert.That(dto.GameId, Is.EqualTo(game.Id));
            Assert.That(dto.GameModeId, Is.EqualTo(game.GameModeId));
            Assert.That(dto.GameModeKey, Is.EqualTo("blitz"));
            Assert.That(dto.Status, Is.EqualTo(GameStatus.Running));
            Assert.That(dto.TimeLimitInSeconds, Is.EqualTo(600));
            Assert.That(dto.EndedAt, Is.Null);
            Assert.That(dto.Participants, Has.Count.EqualTo(2));
            Assert.That(dto.TechStacks, Is.EquivalentTo(new[] { "Python" }));
        });

        GameParticipantDto knownDto = dto.Participants.Single(p => p.UserId == knownUserId);
        Assert.Multiple(() =>
        {
            Assert.That(knownDto.Username, Is.EqualTo("alice"));
            Assert.That(knownDto.ImageUrl, Is.EqualTo("https://example.com/a.png"));
            Assert.That(knownDto.Score, Is.EqualTo(1));
            Assert.That(knownDto.CurrentProblem, Is.Not.Null);
            Assert.That(knownDto.CurrentProblem!.ProblemId, Is.EqualTo(initialProblemId));
            Assert.That(knownDto.HasForfeited, Is.False);
            Assert.That(knownDto.HasFinishedProblems, Is.False);
        });

        GameParticipantDto unknownDto = dto.Participants.Single(p => p.UserId == unknownUserId);
        Assert.Multiple(() =>
        {
            Assert.That(unknownDto.Username, Is.EqualTo(string.Empty));
            Assert.That(unknownDto.ImageUrl, Is.Null);
            Assert.That(unknownDto.CurrentProblem, Is.Null);
        });
    }
}