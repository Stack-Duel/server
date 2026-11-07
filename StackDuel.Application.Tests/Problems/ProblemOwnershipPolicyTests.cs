using Moq;
using StackDuel.Application.Problems;

namespace StackDuel.Application.Tests.Problems;

public class ProblemOwnershipPolicyTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly ProblemOwnershipPolicy _policy;

    public ProblemOwnershipPolicyTests()
    {
        _policy = new ProblemOwnershipPolicy(_problemReadRepository.Object);
    }

    [Fact]
    public async Task IsOwnedByAsync_UserIsCreator_ReturnsTrue()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _problemReadRepository
            .Setup(x => x.GetCreatedByIdAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        bool result = await _policy.IsOwnedByAsync(problemId, userId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsOwnedByAsync_UserIsNotCreator_ReturnsFalse()
    {
        Guid problemId = Guid.NewGuid();

        _problemReadRepository
            .Setup(x => x.GetCreatedByIdAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        bool result = await _policy.IsOwnedByAsync(problemId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsOwnedByAsync_ProblemDoesNotExist_ReturnsFalse()
    {
        Guid problemId = Guid.NewGuid();

        _problemReadRepository
            .Setup(x => x.GetCreatedByIdAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        bool result = await _policy.IsOwnedByAsync(problemId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }
}
