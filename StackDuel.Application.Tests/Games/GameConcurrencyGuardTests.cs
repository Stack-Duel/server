using Moq;
using StackDuel.Application.Games;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Tests.Games;

public class GameConcurrencyGuardTests
{
    [Fact]
    public async Task RunExclusiveAsync_DelegatesToAdvisoryLockUnderAGameNamespacedKey()
    {
        var advisoryLock = new Mock<IAdvisoryLock>();
        var guard = new GameConcurrencyGuard(advisoryLock.Object);
        var gameId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        advisoryLock
            .Setup(x =>
                x.RunExclusiveAsync(
                    "game",
                    gameId,
                    It.IsAny<Func<CancellationToken, Task<int>>>(),
                    cancellationToken
                )
            )
            .Returns<string, Guid, Func<CancellationToken, Task<int>>, CancellationToken>(
                (_, _, action, ct) => action(ct)
            );

        int result = await guard.RunExclusiveAsync(gameId, _ => Task.FromResult(42), cancellationToken);

        Assert.Equal(42, result);
        advisoryLock.Verify(
            x =>
                x.RunExclusiveAsync(
                    "game",
                    gameId,
                    It.IsAny<Func<CancellationToken, Task<int>>>(),
                    cancellationToken
                ),
            Times.Once
        );
    }
}