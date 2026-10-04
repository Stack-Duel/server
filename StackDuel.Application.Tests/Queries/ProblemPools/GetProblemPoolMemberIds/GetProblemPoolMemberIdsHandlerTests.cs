using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;
using Ardalis.Result;
using Moq;
using GetProblemPoolMemberIdsHandler = StackDuel.Application.Queries.ProblemPools.GetProblemPoolMemberIds.GetProblemPoolMemberIdsHandler;
using GetProblemPoolMemberIdsQuery = StackDuel.Application.Queries.ProblemPools.GetProblemPoolMemberIds.GetProblemPoolMemberIdsQuery;

namespace StackDuel.Application.Tests.Queries.ProblemPools.GetProblemPoolMemberIds;

public class GetProblemPoolMemberIdsHandlerTests
{
    private Mock<IProblemPoolRepository> _problemPoolRepository = null!;
    private GetProblemPoolMemberIdsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemPoolRepository = new Mock<IProblemPoolRepository>();
        _handler = new GetProblemPoolMemberIdsHandler(_problemPoolRepository.Object);
    }

    [Test]
    public async Task Handle_PoolNotFound_ReturnsNotFound()
    {
        _problemPoolRepository
            .Setup(x => x.FindByKeyAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemPool?)null);

        Result<IReadOnlyList<Guid>> result = await _handler.Handle(
            new GetProblemPoolMemberIdsQuery("missing"),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_PoolFound_ReturnsItsProblemIds()
    {
        var pool = new ProblemPool("daily", "Daily challenge");
        var problemId = Guid.NewGuid();
        pool.AddProblem(problemId);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("daily", It.IsAny<CancellationToken>())).ReturnsAsync(pool);

        Result<IReadOnlyList<Guid>> result = await _handler.Handle(
            new GetProblemPoolMemberIdsQuery("daily"),
            CancellationToken.None
        );

        Assert.That(result.Value, Is.EquivalentTo(new[] { problemId }));
    }

    [Test]
    public async Task Handle_EmptyPool_ReturnsEmptyList()
    {
        var pool = new ProblemPool("empty", "Empty pool");
        _problemPoolRepository.Setup(x => x.FindByKeyAsync("empty", It.IsAny<CancellationToken>())).ReturnsAsync(pool);

        Result<IReadOnlyList<Guid>> result = await _handler.Handle(
            new GetProblemPoolMemberIdsQuery("empty"),
            CancellationToken.None
        );

        Assert.That(result.Value, Is.Empty);
    }
}