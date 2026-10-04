using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Problems;
using StackDuel.Application.Submissions;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using Ardalis.Result;
using Moq;
using GetTodaysDailyChallengeHandler = StackDuel.Application.Queries.DailyChallenges.GetTodaysDailyChallenge.GetTodaysDailyChallengeHandler;
using GetTodaysDailyChallengeQuery = StackDuel.Application.Queries.DailyChallenges.GetTodaysDailyChallenge.GetTodaysDailyChallengeQuery;

namespace StackDuel.Application.Tests.Queries.DailyChallenges.GetTodaysDailyChallenge;

public class GetTodaysDailyChallengeHandlerTests
{
    private Mock<IDailyChallengeRepository> _dailyChallengeRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private GetTodaysDailyChallengeHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dailyChallengeRepository = new Mock<IDailyChallengeRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();

        _handler = new GetTodaysDailyChallengeHandler(
            _dailyChallengeRepository.Object,
            _problemReadRepository.Object,
            _submissionReadRepository.Object
        );
    }

    private static Problem CreateProblem(string slug, string title, int difficulty) =>
        new(
            new Slug(slug),
            new Title(title),
            new Question(new string('a', 50)),
            new Difficulty(difficulty),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    [Test]
    public async Task Handle_NoChallengeToday_ReturnsNotFound()
    {
        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyChallenge?)null);

        var result = await _handler.Handle(new GetTodaysDailyChallengeQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_TodaySolved_CurrentStreakIncludesToday()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly yesterday = today.AddDays(-1);
        Guid userId = Guid.NewGuid();
        Guid todayProblemId = Guid.NewGuid();
        Guid yesterdayProblemId = Guid.NewGuid();
        var todaysChallenge = new DailyChallenge(today, todayProblemId);
        var yesterdaysChallenge = new DailyChallenge(yesterday, yesterdayProblemId);
        Problem problem = CreateProblem("today-problem", "Today Problem", 50);

        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todaysChallenge);
        _dailyChallengeRepository
            .Setup(x => x.GetAllOrderedByDateDescendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([todaysChallenge, yesterdaysChallenge]);
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(todayProblemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _submissionReadRepository
            .Setup(x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    userId,
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([todayProblemId, yesterdayProblemId]);

        var result = await _handler.Handle(new GetTodaysDailyChallengeQuery(userId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.SolvedByCurrentUser, Is.True);
            Assert.That(result.Value.CurrentStreak, Is.EqualTo(2));
            Assert.That(result.Value.LongestStreak, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Handle_TodayUnsolved_DoesNotBreakExistingStreak()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly yesterday = today.AddDays(-1);
        DateOnly twoDaysAgo = today.AddDays(-2);
        Guid userId = Guid.NewGuid();
        Guid todayProblemId = Guid.NewGuid();
        Guid yesterdayProblemId = Guid.NewGuid();
        Guid twoDaysAgoProblemId = Guid.NewGuid();
        var todaysChallenge = new DailyChallenge(today, todayProblemId);
        var yesterdaysChallenge = new DailyChallenge(yesterday, yesterdayProblemId);
        var twoDaysAgoChallenge = new DailyChallenge(twoDaysAgo, twoDaysAgoProblemId);
        Problem problem = CreateProblem("today-problem", "Today Problem", 50);

        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todaysChallenge);
        _dailyChallengeRepository
            .Setup(x => x.GetAllOrderedByDateDescendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([todaysChallenge, yesterdaysChallenge, twoDaysAgoChallenge]);
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(todayProblemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _submissionReadRepository
            .Setup(x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    userId,
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([yesterdayProblemId, twoDaysAgoProblemId]);

        var result = await _handler.Handle(new GetTodaysDailyChallengeQuery(userId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.SolvedByCurrentUser, Is.False);
            Assert.That(result.Value.CurrentStreak, Is.EqualTo(2));
            Assert.That(result.Value.LongestStreak, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Handle_GapInSolvedHistory_LongestStreakTracksBestRunNotJustCurrent()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid userId = Guid.NewGuid();

        List<DailyChallenge> challenges =
        [
            .. Enumerable.Range(0, 5).Select(i => new DailyChallenge(today.AddDays(-i), Guid.NewGuid())),
        ];
        DailyChallenge todaysChallenge = challenges[0];
        Problem problem = CreateProblem("today-problem", "Today Problem", 50);

        Guid[] solvedIds =
        [
            challenges[0].ProblemId,
            challenges[2].ProblemId,
            challenges[3].ProblemId,
            challenges[4].ProblemId,
        ];

        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todaysChallenge);
        _dailyChallengeRepository
            .Setup(x => x.GetAllOrderedByDateDescendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenges);
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(todaysChallenge.ProblemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _submissionReadRepository
            .Setup(x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    userId,
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(solvedIds);

        var result = await _handler.Handle(new GetTodaysDailyChallengeQuery(userId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.CurrentStreak, Is.EqualTo(1));
            Assert.That(result.Value.LongestStreak, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Handle_FutureChallengeExists_DoesNotAffectStreakOrGetReturned()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly tomorrow = today.AddDays(1);
        Guid userId = Guid.NewGuid();
        Guid tomorrowProblemId = Guid.NewGuid();
        Guid todayProblemId = Guid.NewGuid();
        Guid yesterdayProblemId = Guid.NewGuid();
        var tomorrowsChallenge = new DailyChallenge(tomorrow, tomorrowProblemId);
        var todaysChallenge = new DailyChallenge(today, todayProblemId);
        var yesterdaysChallenge = new DailyChallenge(today.AddDays(-1), yesterdayProblemId);
        Problem problem = CreateProblem("today-problem", "Today Problem", 50);

        _dailyChallengeRepository
            .Setup(x => x.FindByDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todaysChallenge);
        _dailyChallengeRepository
            .Setup(x => x.GetAllOrderedByDateDescendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([tomorrowsChallenge, todaysChallenge, yesterdaysChallenge]);
        _problemReadRepository
            .Setup(x => x.FindByIdAsync(todayProblemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _submissionReadRepository
            .Setup(x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    userId,
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([tomorrowProblemId, todayProblemId, yesterdayProblemId]);

        var result = await _handler.Handle(new GetTodaysDailyChallengeQuery(userId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.ProblemId, Is.EqualTo(problem.Id));
            Assert.That(result.Value.CurrentStreak, Is.EqualTo(2));
            Assert.That(result.Value.LongestStreak, Is.EqualTo(2));
        });
        _submissionReadRepository.Verify(
            x =>
                x.GetAcceptedProblemIdsForUserAsync(
                    userId,
                    It.Is<IReadOnlyCollection<Guid>>(ids => !ids.Contains(tomorrowProblemId)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}