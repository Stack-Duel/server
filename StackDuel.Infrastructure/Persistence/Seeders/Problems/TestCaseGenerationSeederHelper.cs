using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

/// <summary>
/// Enqueues a test case generation request for a seeded problem — shared by every problem
/// seeder so the idempotency check (already generated? already queued?) and console summary
/// look the same regardless of which problem triggered it. Enqueuing, not running: seeders
/// don't need Judge0 available and don't block waiting on it — a background job (see
/// StackDuel.Infrastructure/Jobs/TestCaseGeneration) picks up pending requests on its own
/// schedule.
/// </summary>
internal static class TestCaseGenerationSeederHelper
{
    public static async Task EnqueueIfNeededAsync(
        StackDuelDbContext context,
        ITestCaseGenerationJobRepository jobRepository,
        string problemLabel,
        Guid problemSetupId,
        Guid? existingGenerationSpecId,
        string referenceSolutionCode,
        IReadOnlyList<GenerationParameterSpec> parameters,
        string outputValueType,
        int targetCaseCount,
        int seed,
        CancellationToken cancellationToken
    )
    {
        if (existingGenerationSpecId is not null)
            return;

        bool hasPendingJob = await context
            .Set<TestCaseGenerationJob>()
            .AsNoTracking()
            .AnyAsync(
                j =>
                    j.ProblemSetupId == problemSetupId
                    && (
                        j.Status == TestCaseGenerationJobStatus.Pending
                        || j.Status == TestCaseGenerationJobStatus.Processing
                    ),
                cancellationToken
            );

        if (hasPendingJob)
        {
            Console.WriteLine($"{problemLabel}: test case generation already queued, skipping.");
            return;
        }

        var job = new TestCaseGenerationJob(
            problemSetupId,
            referenceSolutionCode,
            parameters,
            outputValueType,
            targetCaseCount,
            seed
        );

        await jobRepository.AddAsync(job, cancellationToken);

        Console.WriteLine(
            $"{problemLabel}: queued test case generation (target {targetCaseCount} cases, job {job.Id})."
        );
    }
}