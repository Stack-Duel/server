using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using SkipProblemCommand = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemCommand;
using SkipProblemHandler = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemHandler;
using SkipProblemResultDto = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemResultDto;
using SkipProblemValidator = StackDuel.Application.Commands.Games.SkipProblem.SkipProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.SkipProblem;

public class SkipProblemHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IGameExpiryCanceller> _gameExpiryCanceller = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private Mock<IGameProblemSequencer> _gameProblemSequencer = null!;
    private Mock<IGameConcurrencyGuard> _gameConcurrencyGuard = null!;
    private SkipProblemHandler _handler = null!;

    public SkipProblemHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _gameExpiryCanceller = new Mock<IGameExpiryCanceller>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();
        _gameProblemSequencer = new Mock<IGameProblemSequencer>();
        _gameConcurrencyGuard = new Mock<IGameConcurrencyGuard>();

        _strategyResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(_strategy.Object);
        _gameConcurrencyGuard
            .Setup(x =>
                x.RunExclusiveAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Func<CancellationToken, Task<(Result<SkipProblemResultDto>, IReadOnlyList<IDomainEvent>)>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns<
                Guid,
                Func<CancellationToken, Task<(Result<SkipProblemResultDto>, IReadOnlyList<IDomainEvent>)>>,
                CancellationToken
            >((_, action, ct) => action(ct));

        var gameProblemAdvancer = new GameProblemAdvancer(
            _gameProblemSequencer.Object,
            _gameExpiryCanceller.Object,
            _strategyResolver.Object
        );

        _handler = new SkipProblemHandler(
            _gameReadRepository.Object,
            _unitOfWork.Object,
            _domainEventDispatcher.Object,
            gameProblemAdvancer,
            _gameConcurrencyGuard.Object,
            new SkipProblemValidator()
        );
    }

    private static GameMode CreateGameMode(string key = "solo_rush") =>
        new(key, "Solo Rush", "Solo rush mode", true, 1, 1, Guid.NewGuid());

    private static void StartParticipantOnProblem(Game game, Guid userId, Guid problemId)
    {
        game.AppendRound(problemId);
        game.StartRoundFor(userId, problemId);
    }

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
        StartParticipantOnProblem(game, userId, problemId);
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
    public async Task Handle_RoundNotStarted_ReturnsInvalid()
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
        StartParticipantOnProblem(game, userId, Guid.NewGuid());
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
        StartParticipantOnProblem(game, userId, problemId);
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
        StartParticipantOnProblem(game, userId, problemId);
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
        StartParticipantOnProblem(game, userId, problemId);
        var nextProblemId = Guid.NewGuid();
        game.AppendRound(nextProblemId);
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
        Assert.Equal(nextProblemId, game.CurrentProblemIdFor(userId));
        Guid skippedRoundId = game.Rounds.First(r => r.ProblemId == problemId).Id;
        Assert.Contains(
            participant.RoundResults,
            result => result.RoundId == skippedRoundId && result.Outcome == GameRoundOutcome.Skipped
        );
        Assert.Equal(0, participant.Score);
        _gameExpiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SkippingAdvancesThePositionPassedToTheSequencerForDifficultyPacing()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var firstProblemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, firstProblemId);
        var secondProblemId = Guid.NewGuid();
        game.AppendRound(secondProblemId);
        game.SkipCurrentRound(userId);
        game.StartRoundFor(userId, secondProblemId);
        var problemId = game.CurrentProblemIdFor(userId)!.Value;
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
            .ReturnsAsync(() =>
            {
                Guid next = Guid.NewGuid();
                game.AppendRound(next);
                return next;
            });

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        await _handler.Handle(command, CancellationToken.None);

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
        StartParticipantOnProblem(game, userId, problemId);
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
        Assert.Null(participant.CurrentRoundId);

        GameRoundResult finalRound = participant.RoundResults.Single();
        Assert.Equal(GameRoundOutcome.Skipped, finalRound.Outcome);
        Assert.Null(finalRound.SolvingSubmissionId);

        _gameExpiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdvanceFailsWithDuplicateProblem_DoesNotPersistTheAlreadySkippedRound()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        StartParticipantOnProblem(game, userId, problemId);
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
            .Callback<Game, int, string, IProblemSelectionStrategy, CancellationToken>(
                (g, _, _, _, _) => g.AppendRound(problemId)
            )
            .ReturnsAsync(problemId);

        var command = new SkipProblemCommand(game.Id, problemId, userId);
        Result<SkipProblemResultDto> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Error, result.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _domainEventDispatcher.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<StackDuel.Domain.SeedWork.IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_RunsTheWholeOperationUnderTheGameConcurrencyGuard()
    {
        var gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new SkipProblemCommand(gameId, Guid.NewGuid(), Guid.NewGuid());
        await _handler.Handle(command, CancellationToken.None);

        _gameConcurrencyGuard.Verify(
            x =>
                x.RunExclusiveAsync(
                    gameId,
                    It.IsAny<Func<CancellationToken, Task<(Result<SkipProblemResultDto>, IReadOnlyList<IDomainEvent>)>>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}