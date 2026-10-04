using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.Problems.Enums;
using Moq;
using GetUpcomingDailyChallengesHandler = StackDuel.Application.Queries.DailyChallenges.GetUpcomingDailyChallenges.GetUpcomingDailyChallengesHandler;
using GetUpcomingDailyChallengesQuery = StackDuel.Application.Queries.DailyChallenges.GetUpcomingDailyChallenges.GetUpcomingDailyChallengesQuery;

namespace StackDuel.Application.Tests.Queries.DailyChallenges.GetUpcomingDailyChallenges;

public class GetUpcomingDailyChallengesHandlerTests
{
    private Mock<IDailyChallengeRepository> _dailyChallengeRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private GetUpcomingDailyChallengesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dailyChallengeRepository = new Mock<IDailyChallengeRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _handler = new GetUpcomingDailyChallengesHandler(
            _dailyChallengeRepository.Object,
            _problemReadRepository.Object
        );
    }

    [Test]
    public async Task Handle_NoUpcomingChallenges_ReturnsEmptyWithoutQueryingProblems()
    {
        _dailyChallengeRepository
            .Setup(x => x.GetUpcomingAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetUpcomingDailyChallengesQuery(), CancellationToken.None);

        Assert.That(result.Value, Is.Empty);
        _problemReadRepository.Verify(
            x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_UpcomingChallengesExist_MapsProblemDetails()
    {
        var problemId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        _dailyChallengeRepository
            .Setup(x => x.GetUpcomingAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DailyChallenge(date, problemId)]);
        _problemReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AdminProblemListRowDto(
                    problemId,
                    "two-sum",
                    "Two Sum",
                    150,
                    ProblemStatus.Published,
                    1000,
                    256,
                    [],
                    [],
                    0,
                    DateTime.UtcNow,
                    null
                ),
            ]);

        var result = await _handler.Handle(new GetUpcomingDailyChallengesQuery(), CancellationToken.None);

        var dto = result.Value.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dto.Date, Is.EqualTo(date));
            Assert.That(dto.ProblemId, Is.EqualTo(problemId));
            Assert.That(dto.ProblemSlug, Is.EqualTo("two-sum"));
            Assert.That(dto.ProblemTitle, Is.EqualTo("Two Sum"));
            Assert.That(dto.Status, Is.EqualTo(ProblemStatus.Published));
        });
    }

    [Test]
    public async Task Handle_ProblemNoLongerExists_FallsBackToPlaceholder()
    {
        var problemId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        _dailyChallengeRepository
            .Setup(x => x.GetUpcomingAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DailyChallenge(date, problemId)]);
        _problemReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetUpcomingDailyChallengesQuery(), CancellationToken.None);

        var dto = result.Value.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dto.ProblemTitle, Is.EqualTo("(problem no longer exists)"));
            Assert.That(dto.Status, Is.EqualTo(ProblemStatus.Archived));
        });
    }
}