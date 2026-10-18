using Moq;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Tests.Games;

public class GameProblemAdvancerTests
{
    private Mock<IGameProblemSequencer> _sequencer = null!;
    private Mock<IGameExpiryCanceller> _expiryCanceller = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private GameProblemAdvancer _advancer = null!;

    public GameProblemAdvancerTests()
    {
        _sequencer = new Mock<IGameProblemSequencer>();
        _expiryCanceller = new Mock<IGameExpiryCanceller>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();

        _strategyResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(_strategy.Object);

        _advancer = new GameProblemAdvancer(
            _sequencer.Object,
            _expiryCanceller.Object,
            _strategyResolver.Object,
            _gameReadRepository.Object,
            _gameWriteRepository.Object
        );
    }

    private static Game CreateRunningGameWithParticipant(out Guid userId)
    {
        userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var problemId = Guid.NewGuid();
        game.AppendRound(problemId);
        game.StartRoundFor(userId, problemId);
        game.SolveCurrentRound(userId, Guid.NewGuid());
        return game;
    }

    [Fact]
    public async Task AdvanceAsync_NextProblemAvailable_StartsTheParticipantOnIt()
    {
        var game = CreateRunningGameWithParticipant(out var userId);
        var nextProblemId = Guid.NewGuid();
        _sequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    game,
                    It.IsAny<int>(),
                    "solo_rush",
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Game, int, string, IProblemSelectionStrategy, CancellationToken>(
                (g, _, _, _, _) => g.AppendRound(nextProblemId)
            )
            .ReturnsAsync(nextProblemId);

        GameProblemAdvanceResult result = await _advancer.AdvanceAsync(game, userId, "solo_rush", CancellationToken.None);

        Assert.Equal(nextProblemId, result.NextProblemId);
        Assert.Same(game, result.Game);
        Assert.Equal(nextProblemId, game.CurrentProblemIdFor(userId));
        _expiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AdvanceAsync_NoNextProblem_FinishesParticipantAndCancelsScheduledExpiry()
    {
        var game = CreateRunningGameWithParticipant(out var userId);
        _sequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    game,
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Guid?)null);

        GameProblemAdvanceResult result = await _advancer.AdvanceAsync(game, userId, "solo_rush", CancellationToken.None);

        Assert.Null(result.NextProblemId);
        Assert.True(game.Participants.First(p => p.UserId == userId).HasFinishedProblems);
        _expiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_ResolvesStrategyForTheGivenGameModeKey()
    {
        var game = CreateRunningGameWithParticipant(out var userId);
        var nextProblemId = Guid.NewGuid();
        _sequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(game, It.IsAny<int>(), "duel", _strategy.Object, It.IsAny<CancellationToken>())
            )
            .Callback<Game, int, string, IProblemSelectionStrategy, CancellationToken>(
                (g, _, _, _, _) => g.AppendRound(nextProblemId)
            )
            .ReturnsAsync(nextProblemId);

        await _advancer.AdvanceAsync(game, userId, "duel", CancellationToken.None);

        _strategyResolver.Verify(x => x.Resolve("duel"), Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_PositionConflict_CancelsOwnAttemptAndAdoptsTheWinningRoundFromAFreshRead()
    {
        var game = CreateRunningGameWithParticipant(out var userId);
        var ourAttemptProblemId = Guid.NewGuid();
        var winningProblemId = Guid.NewGuid();

        var freshGame = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        freshGame.Start();
        var startingProblemId = Guid.NewGuid();
        freshGame.AppendRound(startingProblemId);
        freshGame.StartRoundFor(userId, startingProblemId);
        freshGame.SolveCurrentRound(userId, Guid.NewGuid());
        freshGame.AppendRound(winningProblemId);

        _sequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(game, It.IsAny<int>(), "duel", _strategy.Object, It.IsAny<CancellationToken>())
            )
            .Callback<Game, int, string, IProblemSelectionStrategy, CancellationToken>(
                (g, _, _, _, _) => g.AppendRound(ourAttemptProblemId)
            )
            .ReturnsAsync(ourAttemptProblemId);
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(freshGame);
        _gameWriteRepository
            .SetupSequence(x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new GameProblemPositionConflictException(Guid.NewGuid(), new InvalidOperationException("conflict"))
            )
            .Returns(Task.CompletedTask);

        GameProblemAdvanceResult result = await _advancer.AdvanceAsync(game, userId, "duel", CancellationToken.None);

        Assert.Equal(winningProblemId, result.NextProblemId);
        Assert.Same(freshGame, result.Game);
        Assert.Equal(winningProblemId, freshGame.CurrentProblemIdFor(userId));
        _sequencer.Verify(
            x =>
                x.GetOrGenerateProblemAsync(
                    It.IsAny<Game>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<IProblemSelectionStrategy>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }
}