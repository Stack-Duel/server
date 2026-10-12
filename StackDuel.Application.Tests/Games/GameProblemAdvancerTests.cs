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

    private static Game CreateRunningGameWithParticipant(out GameParticipant participant)
    {
        Guid userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        participant = game.Participants.First(p => p.UserId == userId);
        participant.InitializeProblemSession(Guid.NewGuid());
        return game;
    }

    [Fact]
    public async Task AdvanceAsync_NextProblemAvailable_InvokesOnAdvanceWithIt()
    {
        var game = CreateRunningGameWithParticipant(out var participant);
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
            .ReturnsAsync(nextProblemId);
        Guid? advancedTo = null;

        Guid? result = await _advancer.AdvanceAsync(
            game,
            participant,
            "solo_rush",
            onAdvance: id => advancedTo = id,
            onFinish: null,
            CancellationToken.None
        );

        Assert.Equal(nextProblemId, result);
        Assert.Equal(nextProblemId, advancedTo);
        Assert.False(participant.HasFinishedProblems);
        _expiryCanceller.Verify(
            x => x.CancelIfScheduledAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AdvanceAsync_NoNextProblem_FinishesParticipantAndCancelsScheduledExpiry()
    {
        var game = CreateRunningGameWithParticipant(out var participant);
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

        Guid? result = await _advancer.AdvanceAsync(
            game,
            participant,
            "solo_rush",
            onAdvance: _ => Assert.Fail("onAdvance should not run when the pool is exhausted."),
            onFinish: null,
            CancellationToken.None
        );

        Assert.Null(result);
        Assert.True(participant.HasFinishedProblems);
        _expiryCanceller.Verify(x => x.CancelIfScheduledAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_NoNextProblem_InvokesOnFinishBeforeMarkingParticipantFinished()
    {
        var game = CreateRunningGameWithParticipant(out var participant);
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
        bool participantAlreadyFinishedWhenOnFinishRan = true;

        await _advancer.AdvanceAsync(
            game,
            participant,
            "solo_rush",
            onAdvance: _ => { },
            onFinish: () => participantAlreadyFinishedWhenOnFinishRan = participant.HasFinishedProblems,
            CancellationToken.None
        );

        // Mirrors SkipProblemHandler's original ordering: UseSkip() ran before FinishProblemsFor,
        // so a skip is always consumed even when the finish was caused by pool exhaustion.
        Assert.False(participantAlreadyFinishedWhenOnFinishRan);
    }

    [Fact]
    public async Task AdvanceAsync_ResolvesStrategyForTheGivenGameModeKey()
    {
        var game = CreateRunningGameWithParticipant(out var participant);
        _sequencer
            .Setup(x =>
                x.GetOrGenerateProblemAsync(
                    game,
                    It.IsAny<int>(),
                    "duel",
                    _strategy.Object,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Guid.NewGuid());

        await _advancer.AdvanceAsync(
            game,
            participant,
            "duel",
            onAdvance: _ => { },
            onFinish: null,
            CancellationToken.None
        );

        _strategyResolver.Verify(x => x.Resolve("duel"), Times.Once);
    }
}