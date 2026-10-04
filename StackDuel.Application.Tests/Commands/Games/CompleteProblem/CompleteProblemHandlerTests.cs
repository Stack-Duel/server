using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;
using CompleteProblemCommand = StackDuel.Application.Commands.Games.CompleteProblem.CompleteProblemCommand;
using CompleteProblemHandler = StackDuel.Application.Commands.Games.CompleteProblem.CompleteProblemHandler;
using CompleteProblemResultDto = StackDuel.Application.Commands.Games.CompleteProblem.CompleteProblemResultDto;
using CompleteProblemValidator = StackDuel.Application.Commands.Games.CompleteProblem.CompleteProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.CompleteProblem;

public class CompleteProblemHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IGameExpiryCanceller> _gameExpiryCanceller = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private Mock<IGameProblemSequencer> _gameProblemSequencer = null!;
    private Mock<ISubmissionWriteRepository> _submissionRepository = null!;
    private CompleteProblemHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _gameExpiryCanceller = new Mock<IGameExpiryCanceller>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();
        _gameProblemSequencer = new Mock<IGameProblemSequencer>();
        _submissionRepository = new Mock<ISubmissionWriteRepository>();

        _strategyResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(_strategy.Object);

        var gameProblemAdvancer = new GameProblemAdvancer(
            _gameProblemSequencer.Object,
            _gameExpiryCanceller.Object,
            _strategyResolver.Object
        );

        _handler = new CompleteProblemHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _domainEventDispatcher.Object,
            gameProblemAdvancer,
            _submissionRepository.Object,
            new CompleteProblemValidator()
        );
    }

    private static Submission CreateAcceptedSubmission(Guid userId)
    {
        var submission = new Submission(userId, Guid.NewGuid(), SubmissionType.Submit, new SourceCode("print(1)"), []);
        submission.Complete();
        return submission;
    }

    private static GameMode CreateGameMode(string key = "solo_rush") =>
        new(key, "Solo Rush", "Solo rush mode", true, 1, 1, Guid.NewGuid());

    [Test]
    public async Task Handle_InvalidCommand_ReturnsInvalid()
    {
        var command = new CompleteProblemCommand(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);

        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new CompleteProblemCommand(gameId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_UserNotParticipant_ReturnsForbidden()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteProblemCommand(game.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Forbidden));
    }

    [Test]
    public async Task Handle_GameNotRunning_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteProblemCommand(game.Id, Guid.NewGuid(), Guid.NewGuid(), userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_ParticipantHasForfeited_ReturnsInvalid()
    {
        var forfeitingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [forfeitingUserId, otherUserId], 600);
        game.Start();
        game.Participants.First(p => p.UserId == forfeitingUserId).Forfeit();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteProblemCommand(game.Id, Guid.NewGuid(), Guid.NewGuid(), forfeitingUserId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_ProblemSessionNotInitialized_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteProblemCommand(game.Id, Guid.NewGuid(), Guid.NewGuid(), userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_ProblemIdDoesNotMatchCurrentProblem_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        participant.InitializeProblemSession(Guid.NewGuid());
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteProblemCommand(game.Id, Guid.NewGuid(), Guid.NewGuid(), userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_SubmissionIdNotActiveSubmission_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new CompleteProblemCommand(game.Id, problemId, Guid.NewGuid(), userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_SubmissionNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        participant.SetActiveSubmission(submissionId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var command = new CompleteProblemCommand(game.Id, problemId, submissionId, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_SubmissionBelongsToDifferentUser_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var submission = CreateAcceptedSubmission(Guid.NewGuid());
        participant.SetActiveSubmission(submission.Id);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var command = new CompleteProblemCommand(game.Id, problemId, submission.Id, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_SubmissionNotAccepted_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var submission = new Submission(userId, Guid.NewGuid(), SubmissionType.Submit, new SourceCode("print(1)"), []);
        participant.SetActiveSubmission(submission.Id);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var command = new CompleteProblemCommand(game.Id, problemId, submission.Id, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_GameModeNoLongerExists_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var submission = CreateAcceptedSubmission(userId);
        participant.SetActiveSubmission(submission.Id);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var command = new CompleteProblemCommand(game.Id, problemId, submission.Id, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_NextProblemAvailable_AdvancesProblemAndReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var submission = CreateAcceptedSubmission(userId);
        participant.SetActiveSubmission(submission.Id);
        var nextProblemId = Guid.NewGuid();
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

        var command = new CompleteProblemCommand(game.Id, problemId, submission.Id, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.NextProblemId, Is.EqualTo(nextProblemId));
            Assert.That(result.Value.NewScore, Is.EqualTo(1));
            Assert.That(participant.ProblemSession!.CurrentProblemId, Is.EqualTo(nextProblemId));
            Assert.That(participant.Score, Is.EqualTo(1));
        });
        _gameExpiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<StackDuel.Domain.SeedWork.IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_PriorSkipsAdvanceThePositionPassedToTheSequencerForDifficultyPacing()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        participant.InitializeProblemSession(Guid.NewGuid());
        participant.SkipToProblem(Guid.NewGuid());
        participant.SkipToProblem(Guid.NewGuid());
        var problemId = participant.ProblemSession!.CurrentProblemId;
        var submission = CreateAcceptedSubmission(userId);
        participant.SetActiveSubmission(submission.Id);
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
            .ReturnsAsync(Guid.NewGuid());

        var command = new CompleteProblemCommand(game.Id, problemId, submission.Id, userId);
        await _handler.Handle(command, CancellationToken.None);

        // Three problems already seen (initial, then two skips) — the fourth slot is position 3.
        Assert.That(capturedPosition, Is.EqualTo(3));
    }

    [Test]
    public async Task Handle_NoNextProblemAvailable_FinishesProblemsAndCancelsScheduledExpiry()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var submission = CreateAcceptedSubmission(userId);
        participant.SetActiveSubmission(submission.Id);
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

        var command = new CompleteProblemCommand(game.Id, problemId, submission.Id, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.NextProblemId, Is.Null);
            Assert.That(participant.HasFinishedProblems, Is.True);
        });
        _gameExpiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_SaveChangesThrowsPositionConflictOnce_RetriesWithAFreshReadAndSucceeds()
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
            var p = g.Participants.First(x => x.UserId == userId);
            p.InitializeProblemSession(problemId);
            p.SetActiveSubmission(submission.Id);
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
        _gameWriteRepository
            .SetupSequence(x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new GameProblemPositionConflictException(Guid.NewGuid(), new InvalidOperationException("conflict"))
            )
            .Returns(Task.CompletedTask);

        var command = new CompleteProblemCommand(Guid.NewGuid(), problemId, submission.Id, userId);
        Result<CompleteProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.NextProblemId, Is.EqualTo(winningProblemId));
        });
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Test]
    public void Handle_SaveChangesThrowsPositionConflictTwice_PropagatesAfterOneRetry()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var gameMode = CreateGameMode();
        var submission = CreateAcceptedSubmission(userId);

        Game BuildReadyGame()
        {
            var g = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
            g.Start();
            var p = g.Participants.First(x => x.UserId == userId);
            p.InitializeProblemSession(problemId);
            p.SetActiveSubmission(submission.Id);
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
            .ReturnsAsync(Guid.NewGuid());
        _gameWriteRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new GameProblemPositionConflictException(Guid.NewGuid(), new InvalidOperationException("conflict"))
            );

        var command = new CompleteProblemCommand(Guid.NewGuid(), problemId, submission.Id, userId);

        Assert.That(
            async () => await _handler.Handle(command, CancellationToken.None),
            Throws.TypeOf<GameProblemPositionConflictException>()
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }
}