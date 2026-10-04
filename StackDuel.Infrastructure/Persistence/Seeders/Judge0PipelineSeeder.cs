using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.ExecutionPipelines.Enums;

namespace StackDuel.Infrastructure.Persistence.Seeders;

/// <summary>
/// Ensures the default Judge0 execution pipeline exists in the database.
/// Problem seeders call <see cref="GetOrCreateAsync"/> to resolve the pipeline id
/// before seeding problem setups.
/// </summary>
internal sealed class Judge0PipelineSeeder(StackDuelDbContext context) : IStaticSeeder
{
    public const string PipelineName = WellKnownExecutionPipelines.DefaultName;

    public async Task SeedAsync(CancellationToken cancellationToken = default) =>
        await GetOrCreateAsync(cancellationToken);

    public async Task<Guid> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var existingPipeline = await context
            .ExecutionPipelines.AsNoTracking()
            .Where(pipeline => pipeline.Name == PipelineName)
            .Select(pipeline => new
            {
                pipeline.Id,
                Steps = pipeline
                    .Steps.Select(step => new ExistingStepInfo(
                        step.Id,
                        step.StepType,
                        step.StepOrder,
                        step.MaxAttempts,
                        step.TimeoutSeconds,
                        step.IsPolling
                    ))
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPipeline is not null)
        {
            // Insert any steps that don't exist yet.
            List<RequiredStep> missingSteps = GetMissingSteps(existingPipeline.Steps);
            foreach (RequiredStep missingStep in missingSteps)
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO execution_pipeline_steps
                    (id, name, step_type, step_order, max_attempts, timeout_seconds, is_polling, pipeline_id)
                    VALUES
                    ({Guid.NewGuid()}, {missingStep.Name}, {(int)
                        missingStep.StepType}, {missingStep.StepOrder}, {missingStep.MaxAttempts}, {missingStep.TimeoutSeconds}, {missingStep.IsPolling}, {existingPipeline.Id})
                    """,
                    cancellationToken
                );
            }

            // Sync configuration for existing steps so changes to RequiredSteps
            // (e.g. timeout_seconds, max_attempts) take effect on next startup
            // rather than being silently ignored.
            foreach (RequiredStep required in RequiredSteps)
            {
                var existing = existingPipeline.Steps.FirstOrDefault(s => s.StepType == required.StepType);
                if (existing is null)
                    continue;

                bool stale =
                    existing.MaxAttempts != required.MaxAttempts
                    || existing.TimeoutSeconds != required.TimeoutSeconds
                    || existing.IsPolling != required.IsPolling;

                if (stale)
                {
                    await context.Database.ExecuteSqlAsync(
                        $"""
                        UPDATE execution_pipeline_steps
                        SET max_attempts = {required.MaxAttempts},
                            timeout_seconds = {required.TimeoutSeconds},
                            is_polling = {required.IsPolling}
                        WHERE id = {existing.Id}
                        """,
                        cancellationToken
                    );
                }
            }

            context.ChangeTracker.Clear();
            return existingPipeline.Id;
        }

        var pipeline = new ExecutionPipeline(
            PipelineName,
            "Executes submissions via Judge0: submit → poll → evaluate."
        );

        foreach (RequiredStep step in RequiredSteps)
            pipeline.AddStep(
                step.StepType,
                step.StepOrder,
                step.MaxAttempts,
                step.TimeoutSeconds,
                step.IsPolling,
                step.Name
            );

        context.ExecutionPipelines.Add(pipeline);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        return pipeline.Id;
    }

    private static List<RequiredStep> GetMissingSteps(IReadOnlyCollection<ExistingStepInfo> existingSteps)
    {
        HashSet<ExecutionPipelineStepType> existingTypes = [.. existingSteps.Select(step => step.StepType)];
        HashSet<int> existingOrders = [.. existingSteps.Select(step => step.StepOrder)];

        return
        [
            .. RequiredSteps.Where(step =>
                !existingTypes.Contains(step.StepType) && !existingOrders.Contains(step.StepOrder)
            ),
        ];
    }

    private sealed record ExistingStepInfo(
        Guid Id,
        ExecutionPipelineStepType StepType,
        int StepOrder,
        int MaxAttempts,
        int TimeoutSeconds,
        bool IsPolling
    );

    private sealed record RequiredStep(
        ExecutionPipelineStepType StepType,
        int StepOrder,
        int MaxAttempts,
        int TimeoutSeconds,
        bool IsPolling,
        string Name
    );

    private static readonly RequiredStep[] RequiredSteps =
    [
        new(ExecutionPipelineStepType.Judge0Execute, 1, 3, 30, false, "Submit to Judge0"),
        new(ExecutionPipelineStepType.Judge0Poll, 2, 30, 1, true, "Poll Judge0 Results"),
        new(ExecutionPipelineStepType.Evaluate, 3, 1, 10, false, "Evaluate Results"),
    ];
}