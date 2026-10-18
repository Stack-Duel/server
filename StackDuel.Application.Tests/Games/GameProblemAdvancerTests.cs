using Moq;
using StackDuel.Application.Games;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Tests.Games;

public class GameProblemAdvancerTests
{
    private Mock<IGameProblemSequencer> _sequencer = null!;
    private Mock<IGameExpiryCanceller> _expiryCanceller = null!;
    private Mock<IProblemSelectionStrategyResolver> _strategyResolver = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private GameProblemAdvancer _advancer = null!;

    public GameProblemAdvancerTests()
    {
        _sequencer = new Mock<IGameProblemSequencer>();
        _expiryCanceller = new Mock<IGameExpiryCanceller>();
        _strategyResolver = new Mock<IProblemSelectionStrategyResolver>();
        _strategy = new Mock<IProblemSelectionStrategy>();

        _strategyResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(_strategy.Object);

        _advancer = new GameProblemAdvancer(_sequencer.Object, _expiryCanceller.Object, _strategyResolver.Object);
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

        Guid? result = await _advancer.AdvanceAsync(game, userId, "solo_rush", CancellationToken.None);

        Assert.Equal(nextProblemId, result);
        Assert.Equal(nextProblemId, game.CurrentProblemIdFor(userId));
        _expiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
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

        Guid? result = await _advancer.AdvanceAsync(game, userId, "solo_rush", CancellationToken.None);

        Assert.Null(result);
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
}