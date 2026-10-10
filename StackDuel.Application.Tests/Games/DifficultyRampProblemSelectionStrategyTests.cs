using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Tests.Games;

public class DifficultyRampProblemSelectionStrategyTests
{
    private const string GameModeKey = "duel";

    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private DifficultyRampProblemSelectionStrategy _strategy = null!;

    public DifficultyRampProblemSelectionStrategyTests()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _strategy = new DifficultyRampProblemSelectionStrategy(GameModeKey, _problemReadRepository.Object);
    }

    [Fact]
    public async Task SelectNextProblemIdAsync_SolverAndSkipperAtSameRound_UseTheSameSelectionSeed()
    {
        var gameId = Guid.NewGuid();
        var poolId = Guid.NewGuid();
        var excludedProblemIds = new[] { Guid.NewGuid() };
        var capturedSeeds = new List<long>();

        _problemReadRepository
            .Setup(r =>
                r.GetRandomProblemIdByDifficultyAsync(
                    poolId,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    excludedProblemIds,
                    It.IsAny<long>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Guid, int, int, IReadOnlyCollection<Guid>, long, IReadOnlyCollection<Guid>, CancellationToken>(
                (_, _, _, _, seed, _, _) => capturedSeeds.Add(seed)
            )
            .ReturnsAsync(Guid.NewGuid());

        await _strategy.SelectNextProblemIdAsync(
            new ProblemSelectionContext(gameId, poolId, GameModeKey, RoundIndex: 1, excludedProblemIds, [])
        );
        await _strategy.SelectNextProblemIdAsync(
            new ProblemSelectionContext(gameId, poolId, GameModeKey, RoundIndex: 1, excludedProblemIds, [])
        );

        Assert.Equal(2, capturedSeeds.Count);
        Assert.Equal(capturedSeeds[1], capturedSeeds[0]);
    }

    [Fact]
    public async Task SelectNextProblemIdAsync_DifferentGames_UseDifferentSelectionSeeds()
    {
        var poolId = Guid.NewGuid();
        var excludedProblemIds = new[] { Guid.NewGuid() };
        var capturedSeeds = new List<long>();

        _problemReadRepository
            .Setup(r =>
                r.GetRandomProblemIdByDifficultyAsync(
                    poolId,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    excludedProblemIds,
                    It.IsAny<long>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Guid, int, int, IReadOnlyCollection<Guid>, long, IReadOnlyCollection<Guid>, CancellationToken>(
                (_, _, _, _, seed, _, _) => capturedSeeds.Add(seed)
            )
            .ReturnsAsync(Guid.NewGuid());

        await _strategy.SelectNextProblemIdAsync(
            new ProblemSelectionContext(Guid.NewGuid(), poolId, GameModeKey, RoundIndex: 1, excludedProblemIds, [])
        );
        await _strategy.SelectNextProblemIdAsync(
            new ProblemSelectionContext(Guid.NewGuid(), poolId, GameModeKey, RoundIndex: 1, excludedProblemIds, [])
        );

        Assert.Equal(2, capturedSeeds.Count);
        Assert.NotEqual(capturedSeeds[1], capturedSeeds[0]);
    }

    [Fact]
    public async Task SelectNextProblemIdAsync_RoundIndexPastFirstBand_UsesTheNextDifficultyBand()
    {
        var poolId = Guid.NewGuid();
        var capturedRanges = new List<(int Min, int Max)>();

        _problemReadRepository
            .Setup(r =>
                r.GetRandomProblemIdByDifficultyAsync(
                    poolId,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<long>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Guid, int, int, IReadOnlyCollection<Guid>, long, IReadOnlyCollection<Guid>, CancellationToken>(
                (_, min, max, _, _, _, _) => capturedRanges.Add((min, max))
            )
            .ReturnsAsync(Guid.NewGuid());

        await _strategy.SelectNextProblemIdAsync(
            new ProblemSelectionContext(Guid.NewGuid(), poolId, GameModeKey, RoundIndex: 3, [], [])
        );

        Assert.Single(capturedRanges);
        Assert.Equal((121, Difficulty.BeginnerMax), capturedRanges[0]);
    }
}