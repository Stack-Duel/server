using StackDuel.Application.Games;
using StackDuel.Application.Languages;
using StackDuel.Domain.Games.Entities;
using Moq;

namespace StackDuel.Application.Tests.Games;

public class GameProblemSequencerTests
{
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private Mock<IProblemSelectionStrategy> _strategy = null!;
    private GameProblemSequencer _sequencer = null!;

    [SetUp]
    public void SetUp()
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

    [Test]
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

        Assert.That(result, Is.EqualTo(existingProblemId));
        _strategy.Verify(
            x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
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

        Assert.That(result, Is.EqualTo(generatedProblemId));
        Assert.That(game.ProblemIdAtPosition(0), Is.EqualTo(generatedProblemId));
    }

    [Test]
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

        Assert.That(result, Is.Null);
        Assert.That(game.ProblemSequence, Is.Empty);
    }

    [Test]
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

        Assert.That(capturedContext, Is.Not.Null);
        Assert.That(capturedContext!.ExcludedProblemIds, Is.EquivalentTo(new[] { firstProblemId, secondProblemId }));
    }

    [Test]
    public async Task GetOrGenerateProblemAsync_PassesThePositionAsTheRoundIndex()
    {
        var game = CreateGame();
        ProblemSelectionContext? capturedContext = null;
        _strategy
            .Setup(x => x.SelectNextProblemIdAsync(It.IsAny<ProblemSelectionContext>(), It.IsAny<CancellationToken>()))
            .Callback<ProblemSelectionContext, CancellationToken>((context, _) => capturedContext = context)
            .ReturnsAsync(Guid.NewGuid());

        await _sequencer.GetOrGenerateProblemAsync(game, 5, "solo_rush", _strategy.Object, CancellationToken.None);

        Assert.That(capturedContext, Is.Not.Null);
        Assert.That(capturedContext!.RoundIndex, Is.EqualTo(5));
    }

    [Test]
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

        Assert.That(capturedContext, Is.Not.Null);
        Assert.That(capturedContext!.AllowedLanguageVersionIds, Is.EquivalentTo(new[] { versionId }));
    }
}