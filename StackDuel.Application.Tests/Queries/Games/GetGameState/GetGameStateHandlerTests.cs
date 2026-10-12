using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Tracks.Entities;
using GetGameStateHandler = StackDuel.Application.Queries.Games.GetGameState.GetGameStateHandler;
using GetGameStateQuery = StackDuel.Application.Queries.Games.GetGameState.GetGameStateQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameState;

public class GetGameStateHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IGameExpiryCanceller> _gameExpiryCanceller = null!;
    private Mock<IGameSubmissionProgressApplier> _gameSubmissionProgressApplier = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private GetGameStateHandler _handler = null!;

    public GetGameStateHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _gameExpiryCanceller = new Mock<IGameExpiryCanceller>();
        _gameSubmissionProgressApplier = new Mock<IGameSubmissionProgressApplier>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _userReadRepository = new Mock<IUserReadRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _trackReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();
        _submissionReadRepository
            .Setup(x => x.GetLatestGameProblemSubmissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new GetGameStateHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _gameExpiryCanceller.Object,
            _gameSubmissionProgressApplier.Object,
            _domainEventDispatcher.Object,
            _userReadRepository.Object,
            _trackReadRepository.Object,
            _submissionReadRepository.Object
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

    private static readonly Guid SharedGameModeId = Guid.NewGuid();

    private static Game CreateRunningGame(params Guid[] userIds)
    {
        var game = new Game(SharedGameModeId, Guid.NewGuid(), [Guid.NewGuid()], userIds, 600);
        game.Start();
        return game;
    }

    private void GivenGame(Game game) =>
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

    private void GivenKnownGameMode() =>
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameMode("blitz", "Blitz", "desc", true, 1, 4, Guid.NewGuid()));

    private void GivenNoUserProfiles() =>
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

    private void GivenLatestSubmissions(params GameProblemSubmissionDto[] latestSubmissions) =>
        _submissionReadRepository
            .Setup(x => x.GetLatestGameProblemSubmissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestSubmissions);

    private void GivenPendingProgressOutcome(GamePendingProgressOutcome outcome) =>
        _gameSubmissionProgressApplier
            .Setup(x =>
                x.ApplyPendingAsync(
                    It.IsAny<Game>(),
                    It.IsAny<IReadOnlyList<GameProblemSubmissionDto>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(outcome);

    [Fact]
    public async Task Handle_RequesterHasALatestSubmissionOnTheirCurrentProblem_ExposesItsState()
    {
        var requesterId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var submittedAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        Game game = CreateRunningGame(requesterId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GivenGame(game);
        GivenKnownGameMode();
        GivenNoUserProfiles();
        GivenLatestSubmissions(
            new GameProblemSubmissionDto(
                submissionId,
                game.Id,
                requesterId,
                problemId,
                SubmissionStatus.Queued,
                submittedAt
            )
        );

        Result<GameStateDto> result = await _handler.Handle(
            new GetGameStateQuery(game.Id, requesterId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        GameProblemSubmissionStateDto? latest = result
            .Value.Participants.Single(p => p.UserId == requesterId)
            .CurrentProblem!.LatestSubmission;
        Assert.NotNull(latest);
        Assert.Equal(submissionId, latest!.SubmissionId);
        Assert.Equal(SubmissionStatus.Queued, latest.Status);
        Assert.Equal(submittedAt, latest.CreatedAt);
        _submissionReadRepository.Verify(
            x => x.GetLatestGameProblemSubmissionsAsync(game.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _gameSubmissionProgressApplier.Verify(
            x =>
                x.ApplyPendingAsync(
                    game,
                    It.Is<IReadOnlyList<GameProblemSubmissionDto>>(s => s.Single().SubmissionId == submissionId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_OpponentHasALatestSubmission_KeepsItHiddenFromTheRequester()
    {
        var requesterId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(requesterId, opponentId);

        foreach (GameParticipant participant in game.Participants)
            participant.InitializeProblemSession(problemId);

        GivenGame(game);
        GivenKnownGameMode();
        GivenNoUserProfiles();
        GivenLatestSubmissions(
            new GameProblemSubmissionDto(
                Guid.NewGuid(),
                game.Id,
                opponentId,
                problemId,
                SubmissionStatus.Running,
                DateTime.UtcNow
            )
        );

        Result<GameStateDto> result = await _handler.Handle(
            new GetGameStateQuery(game.Id, requesterId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Participants.Single(p => p.UserId == opponentId).CurrentProblem!.LatestSubmission);
        Assert.Null(result.Value.Participants.Single(p => p.UserId == requesterId).CurrentProblem!.LatestSubmission);
    }

    [Fact]
    public async Task Handle_RequestersOnlySubmissionIsForAnEarlierProblem_LeavesLatestSubmissionNull()
    {
        var requesterId = Guid.NewGuid();
        var currentProblemId = Guid.NewGuid();
        Game game = CreateRunningGame(requesterId);
        game.Participants.Single().InitializeProblemSession(currentProblemId);

        GivenGame(game);
        GivenKnownGameMode();
        GivenNoUserProfiles();
        GivenLatestSubmissions(
            new GameProblemSubmissionDto(
                Guid.NewGuid(),
                game.Id,
                requesterId,
                Guid.NewGuid(),
                SubmissionStatus.Accepted,
                DateTime.UtcNow
            )
        );

        Result<GameStateDto> result = await _handler.Handle(
            new GetGameStateQuery(game.Id, requesterId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        GameCurrentProblemDto currentProblem = result
            .Value.Participants.Single(p => p.UserId == requesterId)
            .CurrentProblem!;
        Assert.Equal(currentProblemId, currentProblem.ProblemId);
        Assert.Null(currentProblem.LatestSubmission);
    }

    [Fact]
    public async Task Handle_PendingProgressConflicted_ProjectsTheReloadedGame()
    {
        var requesterId = Guid.NewGuid();
        var staleProblemId = Guid.NewGuid();
        var reloadedProblemId = Guid.NewGuid();

        Game staleGame = CreateRunningGame(requesterId);
        staleGame.Participants.Single().InitializeProblemSession(staleProblemId);

        Game reloadedGame = CreateRunningGame(requesterId);
        reloadedGame.Participants.Single().InitializeProblemSession(reloadedProblemId);

        _gameReadRepository
            .SetupSequence(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleGame)
            .ReturnsAsync(reloadedGame);
        GivenKnownGameMode();
        GivenNoUserProfiles();
        GivenLatestSubmissions();
        GivenPendingProgressOutcome(GamePendingProgressOutcome.ConflictedNeedsReload);

        Result<GameStateDto> result = await _handler.Handle(
            new GetGameStateQuery(staleGame.Id, requesterId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(
            reloadedProblemId,
            result.Value.Participants.Single(p => p.UserId == requesterId).CurrentProblem!.ProblemId
        );
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(staleGame.Id, It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_PendingProgressConflictedAndGameVanished_ReturnsNotFound()
    {
        var requesterId = Guid.NewGuid();
        Game game = CreateRunningGame(requesterId);
        game.Participants.Single().InitializeProblemSession(Guid.NewGuid());

        _gameReadRepository
            .SetupSequence(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game)
            .ReturnsAsync((Game?)null);
        GivenKnownGameMode();
        GivenNoUserProfiles();
        GivenLatestSubmissions();
        GivenPendingProgressOutcome(GamePendingProgressOutcome.ConflictedNeedsReload);

        Result<GameStateDto> result = await _handler.Handle(
            new GetGameStateQuery(game.Id, requesterId),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
