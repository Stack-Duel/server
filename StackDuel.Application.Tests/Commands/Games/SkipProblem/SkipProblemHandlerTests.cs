using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using SkipProblemCommand = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemCommand;
using SkipProblemHandler = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemHandler;
using SkipProblemResultDto = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemResultDto;
using SkipProblemValidator = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.SkipProblem;

public class SkipProblemHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IGameExpiryCanceller> _gameExpiryCanceller = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private Mock<IGameProblemSequencer> _gameProblemSequencer = null!;
    private SkipProblemHandler _handler = null!;

    public SkipProblemHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _gameExpiryCanceller = new Mock<IGameExpiryCanceller>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();
        _gameProblemSequencer = new Mock<IGameProblemSequencer>();

        _strategyResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(_strategy.Object);

        var gameProblemAdvancer = new GameProblemAdvancer(
            _gameProblemSequencer.Object,
            _gameExpiryCanceller.Object,
            _strategyResolver.Object
        );

        _handler = new SkipProblemHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _domainEventDispatcher.Object,
            gameProblemAdvancer,
            new SkipProblemValidator()
        );
    }

    private static GameMode CreateGameMode(string key = "solo_rush") =>
        new(key, "Solo Rush", "Solo rush mode", true, 1, 1, Guid.NewGuid());

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalid()
    {
        var command = new SkipProblemCommand(Guid.Empty, Guid.Empty, Guid.Empty);

        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new SkipProblemCommand(gameId, Guid.NewGuid(), Guid.NewGuid());
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UserNotParticipant_ReturnsForbidden()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, Guid.NewGuid(), Guid.NewGuid());
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_GameNotRunning_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, Guid.NewGuid(), userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_SkipsDisabledForGame_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600, skipsEnabled: false);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ParticipantHasForfeited_ReturnsInvalid()
    {
        var forfeitingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [forfeitingUserId, otherUserId], 600);
        game.Start();
        game.Participants.First(p => p.UserId == forfeitingUserId).Forfeit();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, Guid.NewGuid(), forfeitingUserId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemSessionNotInitialized_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, Guid.NewGuid(), userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemIdDoesNotMatchCurrentProblem_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        participant.InitializeProblemSession(Guid.NewGuid());
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, Guid.NewGuid(), userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_NoSkipsRemaining_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        for (int i = 0; i < GameParticipant.TotalSkips; i++)
            participant.UseSkip();
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_GameModeNoLongerExists_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NextProblemAvailable_SkipsToItWithoutAffectingScore()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var nextProblemId = Guid.NewGuid();
        var gameMode = CreateGameMode();

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
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

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nextProblemId, result.Value.NextProblemId);
        Assert.Equal(GameParticipant.TotalSkips - 1, result.Value.SkipsRemaining);
        Assert.Equal(nextProblemId, participant.ProblemSession!.CurrentProblemId);
        Assert.Contains(problemId, participant.ProblemSession.SkippedProblemIds);
        Assert.Equal(0, participant.Score);
        _gameExpiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SkippingAdvancesThePositionPassedToTheSequencerForDifficultyPacing()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        participant.InitializeProblemSession(Guid.NewGuid());
        participant.SkipToProblem(Guid.NewGuid());
        var problemId = participant.ProblemSession!.CurrentProblemId;
        var gameMode = CreateGameMode();
        int? capturedPosition = null;

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
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

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        await _handler.Handle(command, CancellationToken.None);

        // Two problems already seen (the initial one, then the one skipped to) — the third slot
        // is position 2.
        Assert.Equal(2, capturedPosition);
    }

    [Fact]
    public async Task Handle_NoNextProblemAvailable_FinishesProblemsAndCancelsScheduledExpiry()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.First(p => p.UserId == userId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        var gameMode = CreateGameMode();

        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
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

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.NextProblemId);
        Assert.Equal(GameParticipant.TotalSkips - 1, result.Value.SkipsRemaining);
        Assert.True(participant.HasFinishedProblems);
        _gameExpiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveChangesThrowsPositionConflictOnce_RetriesWithAFreshReadAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var winningProblemId = Guid.NewGuid();
        var gameMode = CreateGameMode();
        var gameId = Guid.NewGuid();

        Game BuildReadyGame()
        {
            var g = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
            g.Start();
            g.Participants.First(p => p.UserId == userId).InitializeProblemSession(problemId);
            return g;
        }

        _gameReadRepository
            .SetupSequence(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadyGame())
            .ReturnsAsync(BuildReadyGame());
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
            .ThrowsAsync(new GameProblemPositionConflictException(gameId, new InvalidOperationException("conflict")))
            .Returns(Task.CompletedTask);

        var command = new SkipProblemCommand(gameId, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(winningProblemId, result.Value.NextProblemId);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_SaveChangesThrowsPositionConflictTwice_PropagatesAfterOneRetry()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var gameMode = CreateGameMode();
        var gameId = Guid.NewGuid();

        Game BuildReadyGame()
        {
            var g = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
            g.Start();
            g.Participants.First(p => p.UserId == userId).InitializeProblemSession(problemId);
            return g;
        }

        _gameReadRepository
            .SetupSequence(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadyGame())
            .ReturnsAsync(BuildReadyGame());
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
            .ThrowsAsync(new GameProblemPositionConflictException(gameId, new InvalidOperationException("conflict")));

        var command = new SkipProblemCommand(gameId, problemId, userId);

        await Assert.ThrowsAsync<GameProblemPositionConflictException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }
}