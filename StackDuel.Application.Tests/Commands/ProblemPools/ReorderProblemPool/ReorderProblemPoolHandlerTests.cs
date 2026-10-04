using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;
using Ardalis.Result;
using Moq;
using ReorderProblemPoolCommand = StackDuel.Application.Commands.ProblemPools.ReorderProblemPool.ReorderProblemPoolCommand;
using ReorderProblemPoolHandler = StackDuel.Application.Commands.ProblemPools.ReorderProblemPool.ReorderProblemPoolHandler;
using ReorderProblemPoolValidator = StackDuel.Application.Commands.ProblemPools.ReorderProblemPool.ReorderProblemPoolValidator;

namespace StackDuel.Application.Tests.Commands.ProblemPools.ReorderProblemPool;

public class ReorderProblemPoolHandlerTests
{
    private Mock<IProblemPoolRepository> _problemPoolRepository = null!;
    private ReorderProblemPoolHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemPoolRepository = new Mock<IProblemPoolRepository>();
        _handler = new ReorderProblemPoolHandler(_problemPoolRepository.Object, new ReorderProblemPoolValidator());
    }

    [Test]
    public async Task Handle_PoolNotFound_ReturnsNotFound()
    {
        _problemPoolRepository
            .Setup(x => x.FindByKeyAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemPool?)null);

        var command = new ReorderProblemPoolCommand("missing", [Guid.NewGuid()]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_EmptyProblemIds_ReturnsInvalid()
    {
        var command = new ReorderProblemPoolCommand("pool", []);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Handle_GivenOrderMatchesCurrentMembers_ReordersAndSaves()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);
        pool.AddProblem(third);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);

        var command = new ReorderProblemPoolCommand("pool", [third, first, second]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(pool.ProblemIds, Is.EqualTo(new[] { third, first, second }));
        });
        _problemPoolRepository.Verify(x => x.UpdateAsync(pool, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_MissingAMember_ReturnsInvalidAndDoesNotSave()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        pool.AddProblem(first);
        pool.AddProblem(second);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);

        var command = new ReorderProblemPoolCommand("pool", [first]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _problemPoolRepository.Verify(x => x.UpdateAsync(It.IsAny<ProblemPool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_IncludesUnknownId_ReturnsInvalid()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid member = Guid.NewGuid();
        pool.AddProblem(member);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);

        var command = new ReorderProblemPoolCommand("pool", [Guid.NewGuid()]);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }
}