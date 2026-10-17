using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Application.Tests.Games;

public class GameProblemCompleterTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IGameExpiryCanceller> _gameExpiryCanceller = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private Mock<IGameProblemSequencer> _gameProblemSequencer = null!;
    private Mock<ISubmissionWriteRepository> _submissionRepository = null!;
    private Mock<IGameRoundSubmissionReadRepository> _gameRoundSubmissionReadRepository = null!;
    private GameProblemCompleter _completer = null!;

    public GameProblemCompleterTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _gameExpiryCanceller = new Mock<IGameExpiryCanceller>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();
        _gameProblemSequencer = new Mock<IGameProblemSequencer>();
        _submissionRepository = new Mock<ISubmissionWriteRepository>();
        _gameRoundSubmissionReadRepository = new Mock<IGameRoundSubmissionReadRepository>();

        _strategyResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(_strategy.Object);

        var gameProblemAdvancer = new GameProblemAdvancer(
            _gameProblemSequencer.Object,
            _gameExpiryCanceller.Object,
            _strategyResolver.Object
        );

        _completer = new GameProblemCompleter(
            _gameReadRepository.Object,
            _unitOfWork.Object,
            _domainEventDispatcher.Object,
            gameProblemAdvancer,
            _submissionRepository.Object,
            _gameRoundSubmissionReadRepository.Object
        );
    }

    private void AllowSubmission(Guid submissionId) =>
        _gameRoundSubmissionReadRepository
            .Setup(x =>
                x.IsLatestAttemptAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), submissionId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);

    private static Submission CreateAcceptedSubmission(Guid userId)
    {
        var submission = new Submission(userId, Guid.NewGuid(), SubmissionType.Submit, new SourceCode("print(1)"), []);
        submission.Complete();
        return submission;
    }

    private static GameMode CreateGameMode(string key = "solo_rush") =>
        new(key, "Solo Rush", "Solo rush mode", true, 1, 1, Guid.NewGuid());

    private static void StartParticipantOnProblem(Game game, Guid userId, Guid problemId)
    {
        game.AppendRound(problemId);
        game.StartRoundFor(userId, problemId);
    }

    [Fact]
    public async Task CompleteAsync_GameNotFound_ReturnsNotFound()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            gameId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_UserNotParticipant_ReturnsForbidden()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_GameNotRunning_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_ParticipantHasForfeited_ReturnsInvalid()
    {
        var forfeitingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [forfeitingUserId, otherUserId], 600);
        game.Start();
        game.Participants.First(p => p.UserId == forfeitingUserId).Forfeit();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            forfeitingUserId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_RoundNotStarted_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_ProblemIdDoesNotMatchCurrentProblem_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        StartParticipantOnProblem(game, userId, Guid.NewGuid());
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_SubmissionIdNotActiveSubmission_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            Guid.NewGuid(),
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_SubmissionNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        AllowSubmission(submissionId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            submissionId,
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_SubmissionBelongsToDifferentUser_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        var submission = CreateAcceptedSubmission(Guid.NewGuid());
        AllowSubmission(submission.Id);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            submission.Id,
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_SubmissionNotAccepted_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        var submission = new Submission(userId, Guid.NewGuid(), SubmissionType.Submit, new SourceCode("print(1)"), []);
        AllowSubmission(submission.Id);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            submission.Id,
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_GameModeNoLongerExists_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        var submission = CreateAcceptedSubmission(userId);
        AllowSubmission(submission.Id);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            submission.Id,
            userId,
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_NextProblemAvailable_AdvancesProblemAndReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        var submission = CreateAcceptedSubmission(userId);
        AllowSubmission(submission.Id);
        var nextProblemId = Guid.NewGuid();
        game.AppendRound(nextProblemId);
        var gameMode = CreateGameMode();

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    game,
                    It.IsAny<int>(),
                    gameMode.Key,
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(nextProblemId);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            submission.Id,
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(nextProblemId, result.Value.NextProblemId);
        Assert.Equal(1, result.Value.NewScore);
        Assert.Equal(nextProblemId, game.CurrentProblemIdFor(userId));
        Assert.Equal(1, participant.Score);
        _gameExpiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<StackDuel.Domain.SeedWork.IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CompleteAsync_PriorSkipsAdvanceThePositionPassedToTheSequencerForDifficultyPacing()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var firstProblemId = Guid.NewGuid();
        game.AppendRound(firstProblemId);
        game.StartRoundFor(userId, firstProblemId);
        var secondProblemId = Guid.NewGuid();
        game.AppendRound(secondProblemId);
        game.SkipCurrentRoundAndAdvance(userId, secondProblemId);
        var thirdProblemId = Guid.NewGuid();
        game.AppendRound(thirdProblemId);
        game.SkipCurrentRoundAndAdvance(userId, thirdProblemId);
        var problemId = game.CurrentProblemIdFor(userId)!.Value;
        var submission = CreateAcceptedSubmission(userId);
        AllowSubmission(submission.Id);
        var gameMode = CreateGameMode();
        int? capturedPosition = null;

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    game,
                    It.IsAny<int>(),
                    gameMode.Key,
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Game, int, string, IProblemSelectionStrategy, CancellationToken>(
                (_, position, _, _, _) => capturedPosition = position
            )
            .ReturnsAsync(() =>
            {
                Guid next = Guid.NewGuid();
                game.AppendRound(next);
                return next;
            });

        await _completer.CompleteAsync(game.Id, problemId, submission.Id, userId, CancellationToken.None);

        Assert.Equal(3, capturedPosition);
    }

    [Fact]
    public async Task CompleteAsync_NoNextProblemAvailable_FinishesProblemsAndCancelsScheduledExpiry()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
        var submission = CreateAcceptedSubmission(userId);
        AllowSubmission(submission.Id);
        var gameMode = CreateGameMode();

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    game,
                    It.IsAny<int>(),
                    gameMode.Key,
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Guid?)null);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            game.Id,
            problemId,
            submission.Id,
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.NextProblemId);
        Assert.True(participant.HasFinishedProblems);
        Assert.Null(participant.CurrentRoundId);

        GameRoundResult finalRound = participant.RoundResults.Single();
        Assert.Equal(GameRoundOutcome.Solved, finalRound.Outcome);
        Assert.Equal(submission.Id, finalRound.SolvingSubmissionId);

        _gameExpiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_SaveChangesThrowsPositionConflictOnce_RetriesWithAFreshReadAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var winningProblemId = Guid.NewGuid();
        var gameMode = CreateGameMode();
        var submission = CreateAcceptedSubmission(userId);

        Game BuildReadyGame()
        {
            var g = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
            g.Start();
            g.AppendRound(problemId);
            g.StartRoundFor(userId, problemId);
            AllowSubmission(submission.Id);
            g.AppendRound(winningProblemId);
            return g;
        }

        _gameReadRepository
            .SetupSequence(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadyGame())
            .ReturnsAsync(BuildReadyGame());
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    It.IsAny<Game>(),
                    It.IsAny<int>(),
                    gameMode.Key,
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(winningProblemId);
        _unitOfWork
            .SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new GameProblemPositionConflictException(Guid.NewGuid(), new InvalidOperationException("conflict"))
            )
            .Returns(Task.CompletedTask);

        Result<GameProblemCompletionDto> result = await _completer.CompleteAsync(
            Guid.NewGuid(),
            problemId,
            submission.Id,
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(winningProblemId, result.Value.NextProblemId);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CompleteAsync_SaveChangesThrowsPositionConflictTwice_PropagatesAfterOneRetry()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var gameMode = CreateGameMode();
        var submission = CreateAcceptedSubmission(userId);

        Game BuildReadyGame()
        {
            var g = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
            g.Start();
            g.AppendRound(problemId);
            g.StartRoundFor(userId, problemId);
            AllowSubmission(submission.Id);
            return g;
        }

        _gameReadRepository
            .SetupSequence(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadyGame())
            .ReturnsAsync(BuildReadyGame());
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);
        var generatedProblemId = Guid.NewGuid();
        _gameProblemSequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    It.IsAny<Game>(),
                    It.IsAny<int>(),
                    gameMode.Key,
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Game, int, string, IProblemSelectionStrategy, CancellationToken>(
                (g, _, _, _, _) => g.AppendRound(generatedProblemId)
            )
            .ReturnsAsync(generatedProblemId);
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new GameProblemPositionConflictException(Guid.NewGuid(), new InvalidOperationException("conflict"))
            );

        await Assert.ThrowsAsync<GameProblemPositionConflictException>(() =>
            _completer.CompleteAsync(Guid.NewGuid(), problemId, submission.Id, userId, CancellationToken.None)
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}