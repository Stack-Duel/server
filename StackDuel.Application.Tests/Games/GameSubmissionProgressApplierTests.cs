using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Application.Tests.Support;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Tests.Games;

public class GameSubmissionProgressApplierTests
{
    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly Mock<IGameWriteRepository> _gameWriteRepository = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcher = new();
    private readonly Mock<IGameProblemAdvancer> _gameProblemAdvancer = new();
    private readonly Mock<ISubmissionReadRepository> _submissionReadRepository = new();
    private readonly RecordingLogger<GameSubmissionProgressApplier> _logger = new();
    private readonly GameSubmissionProgressApplier _applier;

    public GameSubmissionProgressApplierTests()
    {
        _applier = new GameSubmissionProgressApplier(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _domainEventDispatcher.Object,
            _gameProblemAdvancer.Object,
            _submissionReadRepository.Object,
            _logger
        );
    }

    private static GameMode CreateGameMode(string key = "solo_rush") =>
        new(key, "Solo Rush", "Solo rush mode", true, 1, 4, Guid.NewGuid());

    private static Game CreateRunningGame(params Guid[] userIds) =>
        CreateRunningGame(timeLimitInSeconds: 600, countdownSeconds: 0, userIds);

    private static Game CreateRunningGame(int timeLimitInSeconds, int countdownSeconds, params Guid[] userIds)
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], userIds, timeLimitInSeconds);
        game.Start(countdownSeconds);
        return game;
    }

    private static GameProblemSubmissionDto AcceptedSubmission(Guid gameId, Guid userId, Guid problemId) =>
        new(Guid.NewGuid(), gameId, userId, problemId, SubmissionStatus.Accepted, DateTime.UtcNow);

    private void GivenSubmission(GameProblemSubmissionDto? submission) =>
        _submissionReadRepository
            .Setup(x => x.FindGameProblemSubmissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

    private void GivenGame(Game? game) =>
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

    private void GivenGameMode(GameMode? gameMode) =>
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);

    private void GivenAdvancerYields(Guid? nextProblemId) =>
        _gameProblemAdvancer
            .Setup(x =>
                x.AdvanceAsync(
                    It.IsAny<Game>(),
                    It.IsAny<GameParticipant>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<Guid>>(),
                    It.IsAny<Action?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (Game _, GameParticipant _, string _, Action<Guid> onAdvance, Action? _, CancellationToken _) =>
                {
                    if (nextProblemId.HasValue)
                        onAdvance(nextProblemId.Value);

                    return Task.FromResult(nextProblemId);
                }
            );

    private void VerifyWarningLogged(int expectedCount) => Assert.Equal(expectedCount, _logger.WarningCount);

    private void VerifyNothingPersisted()
    {
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
    public async Task ApplyAsync_SubmissionNotFound_ReturnsNotApplicableWithUnknownScore()
    {
        GivenSubmission(null);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.NotApplicable, result.Value.Outcome);
        Assert.Null(result.Value.Score);
        VerifyNothingPersisted();
    }

    [Theory]
    [InlineData(SubmissionStatus.Queued)]
    [InlineData(SubmissionStatus.Running)]
    [InlineData(SubmissionStatus.WrongAnswer)]
    public async Task ApplyAsync_SubmissionNotAccepted_ReturnsNotApplicableWithUnknownScore(SubmissionStatus status)
    {
        GivenSubmission(
            new GameProblemSubmissionDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                status,
                DateTime.UtcNow
            )
        );

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.NotApplicable, result.Value.Outcome);
        Assert.Null(result.Value.Score);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ApplyAsync_GameNotFound_ReturnsNotFound()
    {
        GivenSubmission(AcceptedSubmission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        GivenGame(null);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ApplyAsync_SubmissionUserIsNotAParticipant_ReturnsNotApplicableAndWarns()
    {
        Game game = CreateRunningGame(Guid.NewGuid());
        GivenSubmission(AcceptedSubmission(game.Id, Guid.NewGuid(), Guid.NewGuid()));
        GivenGame(game);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.NotApplicable, result.Value.Outcome);
        Assert.Null(result.Value.Score);
        VerifyWarningLogged(1);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyAsync_ProblemSessionNotInitialized_ReturnsNotApplicableWithCurrentScore()
    {
        var userId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        GivenSubmission(AcceptedSubmission(game.Id, userId, Guid.NewGuid()));
        GivenGame(game);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.NotApplicable, result.Value.Outcome);
        Assert.Equal(0, result.Value.Score);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyAsync_SubmissionIsForASupersededProblem_LeavesScoreUntouchedAndDoesNotSave()
    {
        var userId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        GameParticipant participant = game.Participants.Single();
        participant.InitializeProblemSession(Guid.NewGuid());
        participant.IncrementScore();

        GivenSubmission(AcceptedSubmission(game.Id, userId, Guid.NewGuid()));
        GivenGame(game);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.NotApplicable, result.Value.Outcome);
        Assert.Equal(1, result.Value.Score);
        Assert.Equal(1, participant.Score);
        Assert.Null(result.Value.NextProblemId);
        _gameProblemAdvancer.Verify(
            x =>
                x.AdvanceAsync(
                    It.IsAny<Game>(),
                    It.IsAny<GameParticipant>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<Guid>>(),
                    It.IsAny<Action?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyAsync_GameNotRunning_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);
        game.Complete();

        GivenSubmission(AcceptedSubmission(game.Id, userId, problemId));
        GivenGame(game);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyAsync_ParticipantHasStoppedPlaying_ReturnsInvalid()
    {
        var forfeitedUserId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(forfeitedUserId, Guid.NewGuid());
        GameParticipant participant = game.Participants.Single(p => p.UserId == forfeitedUserId);
        participant.InitializeProblemSession(problemId);
        participant.Forfeit();

        GivenSubmission(AcceptedSubmission(game.Id, forfeitedUserId, problemId));
        GivenGame(game);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyAsync_GameModeMissing_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GivenSubmission(AcceptedSubmission(game.Id, userId, problemId));
        GivenGame(game);
        GivenGameMode(null);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyAsync_GameModeDeactivatedMidGame_StillResolvesIt()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GivenSubmission(AcceptedSubmission(game.Id, userId, problemId));
        GivenGame(game);
        GivenGameMode(CreateGameMode());
        GivenAdvancerYields(Guid.NewGuid());

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gameReadRepository.Verify(
            x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _gameReadRepository.Verify(
            x => x.FindGameModeByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ApplyAsync_AcceptedSolveOnCurrentProblem_ScoresAdvancesAndPersists()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var nextProblemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        GameParticipant participant = game.Participants.Single();
        participant.InitializeProblemSession(problemId);

        GivenSubmission(AcceptedSubmission(game.Id, userId, problemId));
        GivenGame(game);
        GivenGameMode(CreateGameMode());
        GivenAdvancerYields(nextProblemId);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.Applied, result.Value.Outcome);
        Assert.Equal(1, result.Value.Score);
        Assert.Equal(nextProblemId, result.Value.NextProblemId);
        Assert.Equal(1, participant.Score);
        Assert.Equal(nextProblemId, participant.ProblemSession!.CurrentProblemId);
        Assert.Contains(problemId, participant.ProblemSession.SolvedProblemIds);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ApplyAsync_GameExpiredWhileGrading_PersistsTheCompletionBeforeFailing()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(timeLimitInSeconds: 600, countdownSeconds: -600, userId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GivenSubmission(AcceptedSubmission(game.Id, userId, problemId));
        GivenGame(game);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(GameStatus.Completed, game.Status);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ApplyAsync_PositionConflict_RetriesAgainstTheReloadedGameAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var nextProblemId = Guid.NewGuid();

        Game BuildGame()
        {
            Game g = CreateRunningGame(userId);
            g.Participants.Single().InitializeProblemSession(problemId);
            return g;
        }

        Game firstGame = BuildGame();
        Game reloadedGame = BuildGame();

        GivenSubmission(AcceptedSubmission(firstGame.Id, userId, problemId));
        GivenGame(firstGame);
        GivenGameMode(CreateGameMode());
        GivenAdvancerYields(nextProblemId);

        _gameWriteRepository
            .SetupSequence(x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GameProblemPositionConflictException(firstGame.Id, new InvalidOperationException("race")))
            .Returns(Task.CompletedTask);
        _gameWriteRepository
            .Setup(x => x.ReloadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadedGame);

        Result<GameSubmissionProgressResult> result = await _applier.ApplyAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameSubmissionProgressOutcome.Applied, result.Value.Outcome);
        Assert.Equal(1, result.Value.Score);
        Assert.Equal(nextProblemId, result.Value.NextProblemId);
        Assert.Equal(1, reloadedGame.Participants.Single().Score);
        _gameWriteRepository.Verify(x => x.ReloadAsync(firstGame.Id, It.IsAny<CancellationToken>()), Times.Once);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(reloadedGame, It.IsAny<CancellationToken>()), Times.Once);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ApplyPendingAsync_NoParticipantHasAPendingSolve_ReturnsNoChangeWithoutQueryingSubmissions()
    {
        var userId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(Guid.NewGuid());

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(game, [], CancellationToken.None);

        Assert.Equal(GamePendingProgressOutcome.NoChange, outcome);
        _submissionReadRepository.VerifyNoOtherCalls();
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyPendingAsync_GameNotRunning_ReturnsNoChange()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);
        game.Complete();

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [AcceptedSubmission(game.Id, userId, problemId)],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.NoChange, outcome);
        VerifyNothingPersisted();
    }

    [Theory]
    [InlineData(SubmissionStatus.Queued)]
    [InlineData(SubmissionStatus.Running)]
    public async Task ApplyPendingAsync_LatestSubmissionIsNotTerminal_ReturnsNoChange(SubmissionStatus status)
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [new GameProblemSubmissionDto(Guid.NewGuid(), game.Id, userId, problemId, status, DateTime.UtcNow)],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.NoChange, outcome);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyPendingAsync_AcceptedSubmissionIsForAnotherProblem_ReturnsNoChange()
    {
        var userId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(Guid.NewGuid());

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [AcceptedSubmission(game.Id, userId, Guid.NewGuid())],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.NoChange, outcome);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyPendingAsync_UnscoredAcceptedSolve_AppliesItOnce()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var nextProblemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        GameParticipant participant = game.Participants.Single();
        participant.InitializeProblemSession(problemId);

        GivenGameMode(CreateGameMode());
        GivenAdvancerYields(nextProblemId);

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [AcceptedSubmission(game.Id, userId, problemId)],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.Applied, outcome);
        Assert.Equal(1, participant.Score);
        Assert.Equal(nextProblemId, participant.ProblemSession!.CurrentProblemId);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ApplyPendingAsync_GameModeMissing_ReturnsNoChangeAndWarns()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GivenGameMode(null);

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [AcceptedSubmission(game.Id, userId, problemId)],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.NoChange, outcome);
        VerifyWarningLogged(1);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task ApplyPendingAsync_PositionConflict_ReturnsConflictedNeedsReloadAfterReloading()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(userId);
        game.Participants.Single().InitializeProblemSession(problemId);

        GivenGameMode(CreateGameMode());
        GivenAdvancerYields(Guid.NewGuid());
        _gameWriteRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GameProblemPositionConflictException(game.Id, new InvalidOperationException("race")));

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [AcceptedSubmission(game.Id, userId, problemId)],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.ConflictedNeedsReload, outcome);
        _gameWriteRepository.Verify(x => x.ReloadAsync(game.Id, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ApplyPendingAsync_TwoParticipantsWithUnscoredSolves_AppliesBothInOneSave()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        Game game = CreateRunningGame(firstUserId, secondUserId);

        foreach (GameParticipant participant in game.Participants)
            participant.InitializeProblemSession(problemId);

        GivenGameMode(CreateGameMode());
        GivenAdvancerYields(Guid.NewGuid());

        GamePendingProgressOutcome outcome = await _applier.ApplyPendingAsync(
            game,
            [AcceptedSubmission(game.Id, firstUserId, problemId), AcceptedSubmission(game.Id, secondUserId, problemId)],
            CancellationToken.None
        );

        Assert.Equal(GamePendingProgressOutcome.Applied, outcome);
        Assert.All(game.Participants, participant => Assert.Equal(1, participant.Score));
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}