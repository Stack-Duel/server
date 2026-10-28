using Microsoft.Extensions.DependencyInjection;
using StackDuel.Domain.SeedWork;

namespace StackDuel.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public sealed class PostgresAdvisoryLockTests(IntegrationTestEnvironment environment)
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RunExclusiveAsync_SameKey_SerializesConcurrentCallers()
    {
        var key = Guid.NewGuid();
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<string> first = RunAsAsync(
            key,
            async _ =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
                return "first";
            }
        );

        await firstEntered.Task.WaitAsync(SignalTimeout);

        Task<string> second = RunAsAsync(
            key,
            _ =>
            {
                secondEntered.SetResult();
                return Task.FromResult("second");
            }
        );

        // The second caller contends for the same Postgres advisory lock key, so it cannot reach
        // its action while the first caller's transaction still holds it - this is enforced by
        // Postgres itself, not by timing, so there is nothing to race against here.
        Assert.False(secondEntered.Task.IsCompleted, "Second caller entered its action while the first still held the lock.");

        releaseFirst.SetResult();

        await secondEntered.Task.WaitAsync(SignalTimeout);
        string[] results = await Task.WhenAll(first, second);

        Assert.Contains("first", results);
        Assert.Contains("second", results);
    }

    [Fact]
    public async Task RunExclusiveAsync_DifferentKeys_DoesNotSerializeUnrelatedCallers()
    {
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<string> first = RunAsAsync(
            Guid.NewGuid(),
            async _ =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
                return "first";
            }
        );

        await firstEntered.Task.WaitAsync(SignalTimeout);

        Task<string> second = RunAsAsync(
            Guid.NewGuid(),
            _ =>
            {
                secondEntered.SetResult();
                return Task.FromResult("second");
            }
        );

        // An unrelated key must be able to enter while the first caller still holds its own lock -
        // if the implementation over-serialized, this would hang until the timeout instead of
        // passing by coincidence.
        await secondEntered.Task.WaitAsync(SignalTimeout);

        releaseFirst.SetResult();
        string[] results = await Task.WhenAll(first, second);

        Assert.Contains("first", results);
        Assert.Contains("second", results);
    }

    private async Task<string> RunAsAsync(Guid key, Func<CancellationToken, Task<string>> action)
    {
        await using AsyncServiceScope scope = environment.Factory.Services.CreateAsyncScope();
        var advisoryLock = scope.ServiceProvider.GetRequiredService<IAdvisoryLockService>();

        return await advisoryLock.RunExclusiveAsync("integration-test", key, action);
    }
}