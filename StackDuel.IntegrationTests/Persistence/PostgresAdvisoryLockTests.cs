using Microsoft.Extensions.DependencyInjection;
using StackDuel.Domain.SeedWork;
using System.Collections.Concurrent;

namespace StackDuel.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public sealed class PostgresAdvisoryLockTests(IntegrationTestEnvironment environment)
{
    [Fact]
    public async Task RunExclusiveAsync_SameKey_SerializesConcurrentCallers()
    {
        var key = Guid.NewGuid();
        ConcurrentBag<(string Caller, DateTime StartedAt, DateTime FinishedAt)> timings = [];

        async Task<string> RunAsAsync(string caller)
        {
            await using AsyncServiceScope scope = environment.Factory.Services.CreateAsyncScope();
            var advisoryLock = scope.ServiceProvider.GetRequiredService<IAdvisoryLock>();

            return await advisoryLock.RunExclusiveAsync(
                "integration-test",
                key,
                async _ =>
                {
                    DateTime startedAt = DateTime.UtcNow;
                    await Task.Delay(200);
                    DateTime finishedAt = DateTime.UtcNow;
                    timings.Add((caller, startedAt, finishedAt));
                    return caller;
                }
            );
        }

        Task<string> first = RunAsAsync("first");
        Task<string> second = RunAsAsync("second");
        string[] results = await Task.WhenAll(first, second);

        Assert.Contains("first", results);
        Assert.Contains("second", results);
        Assert.Equal(2, timings.Count);

        var ordered = timings.OrderBy(t => t.StartedAt).ToArray();
        var (_, firstStartedAt, firstFinishedAt) = ordered[0];
        var (_, secondStartedAt, secondFinishedAt) = ordered[1];

        bool noOverlap = firstFinishedAt <= secondStartedAt || secondFinishedAt <= firstStartedAt;
        Assert.True(noOverlap, "Both callers held the lock for the same key at the same time.");
    }

    [Fact]
    public async Task RunExclusiveAsync_DifferentKeys_DoesNotSerializeUnrelatedCallers()
    {
        ConcurrentBag<(string Caller, DateTime StartedAt, DateTime FinishedAt)> timings = [];

        async Task RunAsAsync(string caller, Guid key)
        {
            await using AsyncServiceScope scope = environment.Factory.Services.CreateAsyncScope();
            var advisoryLock = scope.ServiceProvider.GetRequiredService<IAdvisoryLock>();

            await advisoryLock.RunExclusiveAsync(
                "integration-test",
                key,
                async _ =>
                {
                    DateTime startedAt = DateTime.UtcNow;
                    await Task.Delay(200);
                    DateTime finishedAt = DateTime.UtcNow;
                    timings.Add((caller, startedAt, finishedAt));
                    return caller;
                }
            );
        }

        Task first = RunAsAsync("first", Guid.NewGuid());
        Task second = RunAsAsync("second", Guid.NewGuid());
        await Task.WhenAll(first, second);

        Assert.Equal(2, timings.Count);

        var ordered = timings.OrderBy(t => t.StartedAt).ToArray();
        var (_, firstStartedAt, firstFinishedAt) = ordered[0];
        var (_, secondStartedAt, secondFinishedAt) = ordered[1];

        bool overlapped = firstStartedAt < secondFinishedAt && secondStartedAt < firstFinishedAt;
        Assert.True(overlapped, "Unrelated keys should be able to hold their locks concurrently.");
    }
}