using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.TestCaseGeneration;

internal sealed partial class TestCaseRefreshService(StackDuelDbContext context, ILogger<TestCaseRefreshService> logger)
    : ITestCaseRefreshService
{
    private sealed record SetupCandidate(Guid SetupId, string ReferenceSolutionCode);

    public async Task<int> EnqueueRefreshesAsync(CancellationToken cancellationToken = default)
    {
        // Only published problems that already declare a generation recipe and have been
        // generated at least once (GenerationSpecId set) are eligible — a setup with no
        // reference solution or no prior spec has nothing for a refresh to re-roll.
        var candidates = await context
            .Set<Problem>()
            .Where(p => p.Status == ProblemStatus.Published && p.GenerationSpec != null)
            .Select(p => new
            {
                p.GenerationSpec!.Parameters,
                p.GenerationSpec.OutputValueType,
                p.GenerationSpec.TargetCaseCount,
                Setups = p
                    .Setups.Where(s => s.GenerationSpecId != null && s.ReferenceSolutionCode != null)
                    .Select(s => new SetupCandidate(s.Id, s.ReferenceSolutionCode!))
                    .ToList(),
            })
            .Where(p => p.Setups.Count > 0)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            LogNoCandidates();
            return 0;
        }

        List<Guid> candidateSetupIds = [.. candidates.SelectMany(c => c.Setups).Select(s => s.SetupId)];

        HashSet<Guid> busySetupIds =
        [
            .. await context
                .Set<TestCaseGenerationJob>()
                .Where(j =>
                    candidateSetupIds.Contains(j.ProblemSetupId)
                    && (j.Status == TestCaseGenerationJobStatus.Pending || j.Status == TestCaseGenerationJobStatus.Processing)
                )
                .Select(j => j.ProblemSetupId)
                .ToListAsync(cancellationToken),
        ];

        List<TestCaseGenerationJob> jobs = [];
        foreach (var candidate in candidates)
        {
            foreach (SetupCandidate setup in candidate.Setups)
            {
                if (busySetupIds.Contains(setup.SetupId))
                {
                    LogSkippedBusy(setup.SetupId);
                    continue;
                }

                jobs.Add(
                    new TestCaseGenerationJob(
                        setup.SetupId,
                        setup.ReferenceSolutionCode,
                        candidate.Parameters,
                        candidate.OutputValueType,
                        candidate.TargetCaseCount,
                        seed: Random.Shared.Next()
                    )
                );
            }
        }

        if (jobs.Count == 0)
        {
            LogNoCandidates();
            return 0;
        }

        await context.TestCaseGenerationJobs.AddRangeAsync(jobs, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        LogEnqueued(jobs.Count);
        return jobs.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No eligible problem setups found for weekly test case refresh")]
    private partial void LogNoCandidates();

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping weekly refresh for setup {SetupId}: a generation job is already pending or processing"
    )]
    private partial void LogSkippedBusy(Guid setupId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enqueued {Count} test case generation job(s) for weekly refresh")]
    private partial void LogEnqueued(int count);
}