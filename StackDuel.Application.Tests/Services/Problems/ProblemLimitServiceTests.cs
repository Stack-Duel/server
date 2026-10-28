using Moq;
using StackDuel.Application.Problems;
using StackDuel.Application.Services.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Tests.Services.Problems;

public class ProblemLimitServiceTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly ProblemLimitService _service;

    public ProblemLimitServiceTests()
    {
        _service = new ProblemLimitService(_problemReadRepository.Object);
    }

    [Fact]
    public async Task HasReachedOpenProblemLimitAsync_BelowLimit_ReturnsFalse()
    {
        _problemReadRepository
            .Setup(x => x.CountUnresolvedProblemsByCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Problem.MaxOpenProblemsPerCreator - 1);

        bool result = await _service.HasReachedOpenProblemLimitAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HasReachedOpenProblemLimitAsync_AtLimit_ReturnsTrue()
    {
        _problemReadRepository
            .Setup(x => x.CountUnresolvedProblemsByCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Problem.MaxOpenProblemsPerCreator);

        bool result = await _service.HasReachedOpenProblemLimitAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HasReachedOpenProblemLimitAsync_AboveLimit_ReturnsTrue()
    {
        _problemReadRepository
            .Setup(x => x.CountUnresolvedProblemsByCreatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Problem.MaxOpenProblemsPerCreator + 1);

        bool result = await _service.HasReachedOpenProblemLimitAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
    }
}