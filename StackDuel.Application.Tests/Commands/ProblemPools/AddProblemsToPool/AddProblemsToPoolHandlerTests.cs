using Ardalis.Result;
using Moq;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using AddProblemsToPoolCommand = StackDuel.Application.Commands.ProblemPools.AddProblemsToPool.AddProblemsToPoolCommand;
using AddProblemsToPoolHandler = StackDuel.Application.Commands.ProblemPools.AddProblemsToPool.AddProblemsToPoolHandler;
using AddProblemsToPoolValidator = StackDuel.Application.Commands.ProblemPools.AddProblemsToPool.AddProblemsToPoolValidator;

namespace StackDuel.Application.Tests.Commands.ProblemPools.AddProblemsToPool;

public class AddProblemsToPoolHandlerTests
{
    private Mock<IProblemPoolRepository> _problemPoolRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private AddProblemsToPoolHandler _handler = null!;

    public AddProblemsToPoolHandlerTests()
    {
        _problemPoolRepository = new Mock<IProblemPoolRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();

        _handler = new AddProblemsToPoolHandler(
            _problemPoolRepository.Object,
            _problemReadRepository.Object,
            new AddProblemsToPoolValidator()
        );
    }

    private static AdminProblemListRowDto CreateRow(Guid id) =>
        new(id, $"slug-{id}", $"Title {id}", 100, ProblemStatus.Published, 1000, 64, [], [], 1, DateTime.UtcNow, null);

    [Fact]
    public async Task Handle_PoolNotFound_ReturnsNotFound()
    {
        _problemPoolRepository
            .Setup(x => x.FindByKeyAsync("missing-pool", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemPool?)null);

        var command = new AddProblemsToPoolCommand("missing-pool", [Guid.NewGuid()], false, null, []);
        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ExplicitIds_NoSelection_ReturnsInvalid()
    {
        var command = new AddProblemsToPoolCommand("pool", [], false, null, []);
        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ExplicitIds_AddsOnlyExistingProblemsAndReturnsDelta()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid existingId1 = Guid.NewGuid();
        Guid existingId2 = Guid.NewGuid();
        Guid staleId = Guid.NewGuid();

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);
        _problemReadRepository
            .Setup(x =>
                x.FindByIdsAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 3), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([CreateRow(existingId1), CreateRow(existingId2)]);

        var command = new AddProblemsToPoolCommand("pool", [existingId1, existingId2, staleId], false, null, []);
        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equivalent(new[] { existingId1, existingId2 }, pool.ProblemIds, strict: true);
        _problemPoolRepository.Verify(x => x.UpdateAsync(pool, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExplicitIds_AlreadyMemberDoesNotCountTowardDelta()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid alreadyMemberId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        pool.AddProblem(alreadyMemberId);

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);
        _problemReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRow(alreadyMemberId), CreateRow(newId)]);

        var command = new AddProblemsToPoolCommand("pool", [alreadyMemberId, newId], false, null, []);
        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Equivalent(new[] { alreadyMemberId, newId }, pool.ProblemIds, strict: true);
    }

    [Fact]
    public async Task Handle_SelectAllMatching_ExcludesGivenIdsAndAddsRest()
    {
        var pool = new ProblemPool("pool", "Pool");
        Guid keep1 = Guid.NewGuid();
        Guid keep2 = Guid.NewGuid();
        Guid excluded = Guid.NewGuid();

        _problemPoolRepository.Setup(x => x.FindByKeyAsync("pool", It.IsAny<CancellationToken>())).ReturnsAsync(pool);
        _problemReadRepository
            .Setup(x => x.GetAdminProblemIdsMatchingAsync("array", It.IsAny<CancellationToken>()))
            .ReturnsAsync([keep1, keep2, excluded]);

        var command = new AddProblemsToPoolCommand("pool", [], true, "array", [excluded]);
        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equivalent(new[] { keep1, keep2 }, pool.ProblemIds, strict: true);
        Assert.DoesNotContain(excluded, pool.ProblemIds);
        _problemReadRepository.Verify(
            x => x.FindByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}