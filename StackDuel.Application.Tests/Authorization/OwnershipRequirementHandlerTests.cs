using Moq;
using StackDuel.Application.Authorization;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Tests.Authorization;

public class OwnershipRequirementHandlerTests
{
    private readonly Mock<IOwnershipPolicy<Problem>> _ownershipPolicy = new();
    private readonly OwnershipRequirementHandler<Problem> _handler;

    public OwnershipRequirementHandlerTests()
    {
        _handler = new OwnershipRequirementHandler<Problem>(_ownershipPolicy.Object);
    }

    [Fact]
    public async Task HandleAsync_PolicyReturnsTrue_SucceedsTheRequirement()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var requirement = new OwnershipRequirement<Problem>(problemId, userId);

        _ownershipPolicy
            .Setup(x => x.IsOwnedByAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var context = new AuthorizationContext([requirement], CancellationToken.None);

        await _handler.HandleAsync(context);

        Assert.True(context.AllSucceeded);
    }

    [Fact]
    public async Task HandleAsync_PolicyReturnsFalse_DoesNotSucceedTheRequirement()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var requirement = new OwnershipRequirement<Problem>(problemId, userId);

        _ownershipPolicy
            .Setup(x => x.IsOwnedByAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var context = new AuthorizationContext([requirement], CancellationToken.None);

        await _handler.HandleAsync(context);

        Assert.False(context.AllSucceeded);
    }

    [Fact]
    public async Task HandleAsync_IgnoresUnrelatedRequirements()
    {
        var unrelated = new PermissionRequirement("problem:delete");
        var context = new AuthorizationContext([unrelated], CancellationToken.None);

        await _handler.HandleAsync(context);

        _ownershipPolicy.VerifyNoOtherCalls();
    }
}
