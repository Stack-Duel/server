using Moq;
using StackDuel.Application.ProblemValidation;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Tests.ProblemValidation;

public class ProblemValidationConcurrencyGuardTests
{
    private readonly Mock<IAdvisoryLockService> _advisoryLockService = new();
    private readonly IProblemValidationConcurrencyGuard _guard;

    public ProblemValidationConcurrencyGuardTests()
    {
        _guard = new ProblemValidationConcurrencyGuard(_advisoryLockService.Object);

        _advisoryLockService
            .Setup(service =>
                service.RunExclusiveAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Func<CancellationToken, Task<int>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (string _, Guid _, Func<CancellationToken, Task<int>> action, CancellationToken token) =>
                    action(token)
            );
    }

    [Fact]
    public async Task TheGuard_RunsTheActionUnderAProblemScopedLock()
    {
        Guid problemId = Guid.NewGuid();

        int result = await _guard.RunExclusiveAsync(problemId, _ => Task.FromResult(42));

        Assert.Equal(42, result);
        _advisoryLockService.Verify(
            service =>
                service.RunExclusiveAsync(
                    "problem-validation",
                    problemId,
                    It.IsAny<Func<CancellationToken, Task<int>>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task TheCallersToken_IsHandedToTheLockService()
    {
        using CancellationTokenSource cancellation = new();

        await _guard.RunExclusiveAsync(Guid.NewGuid(), _ => Task.FromResult(1), cancellation.Token);

        _advisoryLockService.Verify(
            service =>
                service.RunExclusiveAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Func<CancellationToken, Task<int>>>(),
                    cancellation.Token
                ),
            Times.Once
        );
    }
}