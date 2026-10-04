using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using Ardalis.Result;
using Moq;
using GetOrderedProblemPoolMembersHandler = StackDuel.Application.Queries.ProblemPools.GetOrderedProblemPoolMembers.GetOrderedProblemPoolMembersHandler;
using GetOrderedProblemPoolMembersQuery = StackDuel.Application.Queries.ProblemPools.GetOrderedProblemPoolMembers.GetOrderedProblemPoolMembersQuery;

namespace StackDuel.Application.Tests.Queries.ProblemPools.GetOrderedProblemPoolMembers;

public class GetOrderedProblemPoolMembersHandlerTests
{
    private Mock<IProblemPoolRepository> _problemPoolRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private GetOrderedProblemPoolMembersHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemPoolRepository = new Mock<IProblemPoolRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _handler = new GetOrderedProblemPoolMembersHandler(_problemPoolRepository.Object, _problemReadRepository.Object);
    }

    private static AdminProblemListRowDto CreateRow(Guid id) =>
        new(id, $"slug-{id}", $"Title {id}", 100, ProblemStatus.Published, 1000, 64, [], [], 1, DateTime.UtcNow, null);

    [Test]
    public async Task Handle_PoolNotFound_ReturnsNotFound()
    {
        _problemPoolRepository
            .Setup(x => x.FindByKeyAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemPool?)null);

        Result<IReadOnlyList<AdminProblemListRowDto>> result = await _handler.Handle(
            new GetOrderedProblemPoolMembersQuery("missing"),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_ReturnsMembersInPoolOrder()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);
        _problemReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRow(second), CreateRow(first)]);

        Result<IReadOnlyList<AdminProblemListRowDto>> result = await _handler.Handle(
            new GetOrderedProblemPoolMembersQuery("pool"),
            CancellationToken.None
        );

        Assert.That(result.Value.Select(row => row.Id), Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public async Task Handle_MemberMissingFromProblemLookup_IsOmitted()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid first = Guid.NewGuid();
        Guid deleted = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(deleted);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);
        _problemReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRow(first)]);

        Result<IReadOnlyList<AdminProblemListRowDto>> result = await _handler.Handle(
            new GetOrderedProblemPoolMembersQuery("pool"),
            CancellationToken.None
        );

        Assert.That(result.Value.Select(row => row.Id), Is.EqualTo(new[] { first }));
    }
}