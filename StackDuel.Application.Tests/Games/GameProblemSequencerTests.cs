using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Languages;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Tests.Games;

public class GameProblemSequencerTests
{
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private GameProblemSequencer _sequencer = null!;

    public GameProblemSequencerTests()
    {
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _strategy = new Mock<IProblemSelectionStrategy>();

        _languageReadRepository
            .Setup(x =>
                x.GetLanguageVersionIdsByLanguageIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        _sequencer = new GameProblemSequencer(_languageReadRepository.Object);
    }

    private static Game CreateGame(
        Guid? trackId = null,
        Dictionary<Guid, IReadOnlyList<Guid>>? trackLanguageIds = null
    ) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [trackId ?? Guid.NewGuid()],
            [Guid.NewGuid()],
            600,
            trackLanguageIds: trackLanguageIds
        );

    [Fact]
    public async Task GetOrGenerateProblemAsync_PositionAlreadyInSequence_ReusesItWithoutCallingTheStrategy()
    {
        var game = CreateGame();
        var existingProblemId = Guid.NewGuid();
        game.AppendProblem(existingProblemId);

        Guid? result = await _sequencer.GetOrGenerateProblemAsync(
            game,
            0,
            "solo_rush",
            _strategy.Object,
            CancellationToken.None
        );

        Assert.Equal(existingProblemId, result);
        _strategy.Verify(
            x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetOrGenerateProblemAsync_PositionNotYetInSequence_GeneratesAndAppendsIt()
    {
        var game = CreateGame();
        var generatedProblemId = Guid.NewGuid();
        _strategy
            .Setup(x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedProblemId);

        Guid? result = await _sequencer.GetOrGenerateProblemAsync(
            game,
            0,
            "solo_rush",
            _strategy.Object,
            CancellationToken.None
        );

        Assert.Equal(generatedProblemId, result);
        Assert.Equal(generatedProblemId, game.ProblemIdAtPosition(0));
    }

    [Fact]
    public async Task GetOrGenerateProblemAsync_StrategyReturnsNull_DoesNotAppendAndReturnsNull()
    {
        var game = CreateGame();
        _strategy
            .Setup(x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        Guid? result = await _sequencer.GetOrGenerateProblemAsync(
            game,
            0,
            "solo_rush",
            _strategy.Object,
            CancellationToken.None
        );

        Assert.Null(result);
        Assert.Empty(game.ProblemSequence);
    }

    [Fact]
    public async Task GetOrGenerateProblemAsync_ExcludesProblemsAlreadyInTheSharedSequence()
    {
        var game = CreateGame();
        var firstProblemId = Guid.NewGuid();
        var secondProblemId = Guid.NewGuid();
        game.AppendProblem(firstProblemId);
        game.AppendProblem(secondProblemId);
        ProblemSelectionContext? capturedContext = null;
        _strategy
            .Setup(x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()))
            .Callback<ProblemSelectionContext, CancellationToken>((context, _) => capturedContext = context)
            .ReturnsAsync(Guid.NewGuid());

        await _sequencer.GetOrGenerateProblemAsync(game, 2, "solo_rush", _strategy.Object, CancellationToken.None);

        Assert.NotNull(capturedContext);
        Assert.Equivalent(new[] { firstProblemId, secondProblemId }, capturedContext!.ExcludedProblemIds, strict: true);
    }

    [Fact]
    public async Task GetOrGenerateProblemAsync_PassesThePositionAsTheRoundIndex()
    {
        var game = CreateGame();
        ProblemSelectionContext? capturedContext = null;
        _strategy
            .Setup(x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()))
            .Callback<ProblemSelectionContext, CancellationToken>((context, _) => capturedContext = context)
            .ReturnsAsync(Guid.NewGuid());

        await _sequencer.GetOrGenerateProblemAsync(game, 5, "solo_rush", _strategy.Object, CancellationToken.None);

        Assert.NotNull(capturedContext);
        Assert.Equal(5, capturedContext!.RoundIndex);
    }

    [Fact]
    public async Task GetOrGenerateProblemAsync_ResolvesSelectedTrackLanguagesIntoAllowedLanguageVersionIds()
    {
        var trackId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var game = CreateGame(trackId, new Dictionary<Guid, IReadOnlyList<Guid>> { [trackId] = [languageId] });
        _languageReadRepository
            .Setup(x =>
                x.GetLanguageVersionIdsByLanguageIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => ids.Contains(languageId)),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([versionId]);
        ProblemSelectionContext? capturedContext = null;
        _strategy
            .Setup(x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()))
            .Callback<ProblemSelectionContext, CancellationToken>((context, _) => capturedContext = context)
            .ReturnsAsync(Guid.NewGuid());

        await _sequencer.GetOrGenerateProblemAsync(game, 0, "solo_rush", _strategy.Object, CancellationToken.None);

        Assert.NotNull(capturedContext);
        Assert.Equivalent(new[] { versionId }, capturedContext!.AllowedLanguageVersionIds, strict: true);
    }
}