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

    public GetGameStateHandlerTests()
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

    [Fact]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var query = new GetGameStateQuery(Guid.NewGuid(), Guid.NewGuid());
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_RequestedByNonParticipant_ReturnsForbidden()
    {
        var participantId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantId], 300);

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var query = new GetGameStateQuery(game.Id, Guid.NewGuid());
        Result<GameStateDto> result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
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

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task Handle_EveryoneStopped_SelfHealsCompletionAndCancelsScheduledExpiry()
    {
        var participantAId = Guid.NewGuid();
        var participantBId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [participantAId, participantBId], 300);
        game.Start();

        foreach (GameParticipant participant in game.Participants)
            participant.Forfeit();

        // sanity: race simulated by forfeiting without Game.Forfeit's own self-heal
        Assert.Equal(GameStatus.Running, game.Status);

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

        Assert.True(result.IsSuccess);
        Assert.Equal(GameStatus.Completed, game.Status);
        Assert.Equal(GameStatus.Completed, result.Value.Status);

        _gameExpiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
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

        Assert.True(result.IsSuccess);
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
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

        Assert.True(result.IsSuccess);
        GameStateDto dto = result.Value;

        Assert.Equal(game.Id, dto.GameId);
        Assert.Equal(game.GameModeId, dto.GameModeId);
        Assert.Equal("blitz", dto.GameModeKey);
        Assert.Equal(GameStatus.Running, dto.Status);
        Assert.Equal(600, dto.TimeLimitInSeconds);
        Assert.Null(dto.EndedAt);
        Assert.Equal(2, dto.Participants.Count);
        Assert.Equivalent(new[] { "Python" }, dto.TechStacks, strict: true);

        GameParticipantDto knownDto = dto.Participants.Single(p => p.UserId == knownUserId);
        Assert.Equal("alice", knownDto.Username);
        Assert.Equal("https://example.com/a.png", knownDto.ImageUrl);
        Assert.Equal(1, knownDto.Score);
        Assert.NotNull(knownDto.CurrentProblem);
        Assert.Equal(initialProblemId, knownDto.CurrentProblem!.ProblemId);
        Assert.False(knownDto.HasForfeited);
        Assert.False(knownDto.HasFinishedProblems);

        GameParticipantDto unknownDto = dto.Participants.Single(p => p.UserId == unknownUserId);
        Assert.Equal(string.Empty, unknownDto.Username);
        Assert.Null(unknownDto.ImageUrl);
        Assert.Null(unknownDto.CurrentProblem);
    }
}