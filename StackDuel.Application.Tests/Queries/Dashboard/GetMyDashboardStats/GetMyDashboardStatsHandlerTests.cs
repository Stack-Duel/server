using Ardalis.Result;
using Moq;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Games;
using StackDuel.Application.Queries.Dashboard.GetMyDashboardStats;
using StackDuel.Application.Submissions;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Tests.Queries.Dashboard.GetMyDashboardStats;

public class GetMyDashboardStatsHandlerTests
{
    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly Mock<IDailyChallengeRepository> _dailyChallengeRepository = new();
    private readonly Mock<ISubmissionReadRepository> _submissionReadRepository = new();
    private readonly GetMyDashboardStatsHandler _handler;

    public GetMyDashboardStatsHandlerTests()
    {
        _handler = new GetMyDashboardStatsHandler(
            _gameReadRepository.Object,
            _dailyChallengeRepository.Object,
            _submissionReadRepository.Object
        );

        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[]);
        _dailyChallengeRepository
            .Setup(x => x.GetAllOrderedByDateDescendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<DailyChallenge>)[]);
        _submissionReadRepository
            .Setup(x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyList<Guid>)[]);
    }

    [Fact]
    public async Task Handle_NoGamesAndNoChallenges_ReturnsZeroes()
    {
        Result<UserDashboardStatsDto> result = await _handler.Handle(
            new GetMyDashboardStatsQuery(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(new UserDashboardStatsDto(0, 0, 0), result.Value);
    }

    [Fact]
    public async Task Handle_SumsWinsAcrossGameModes()
    {
        Guid userId = Guid.NewGuid();
        var duel = new GameMode("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid());
        var ffa = new GameMode("ffa", "Free for all", "Everyone", true, 2, 4, Guid.NewGuid());

        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[duel, ffa]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[CompletedWin(duel.Id, userId), CompletedWin(ffa.Id, userId)]);

        Result<UserDashboardStatsDto> result = await _handler.Handle(
            new GetMyDashboardStatsQuery(userId),
            CancellationToken.None
        );

        Assert.Equal(2, result.Value.Wins);
    }

    [Fact]
    public async Task Handle_ReturnsDailyChallengeStreaks()
    {
        Guid userId = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid solvedToday = Guid.NewGuid();
        Guid solvedYesterday = Guid.NewGuid();
        Guid missedTwoDaysAgo = Guid.NewGuid();

        _dailyChallengeRepository
            .Setup(x => x.GetAllOrderedByDateDescendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlyList<DailyChallenge>)
                    [
                        new DailyChallenge(today, solvedToday),
                        new DailyChallenge(today.AddDays(-1), solvedYesterday),
                        new DailyChallenge(today.AddDays(-2), missedTwoDaysAgo),
                    ]
            );
        _submissionReadRepository
            .Setup(x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    userId,
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyList<Guid>)[solvedToday, solvedYesterday]);

        Result<UserDashboardStatsDto> result = await _handler.Handle(
            new GetMyDashboardStatsQuery(userId),
            CancellationToken.None
        );

        Assert.Equal(2, result.Value.CurrentStreak);
        Assert.Equal(2, result.Value.LongestStreak);
    }

    private static Game CompletedWin(Guid gameModeId, Guid winnerUserId)
    {
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [winnerUserId, Guid.NewGuid()], 600);
        game.Start();
        game.RecordProblemSolved(winnerUserId);
        game.Complete();

        return game;
    }
}