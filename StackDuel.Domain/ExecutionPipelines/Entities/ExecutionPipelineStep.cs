using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ExecutionPipelines.Entities;

public sealed class ExecutionPipelineStep : Entity
{
    internal ExecutionPipelineStep(
        ExecutionPipelineStepType stepType,
        int stepOrder,
        int maxAttempts,
        int timeoutSeconds,
        bool isPolling,
        string name
    )
    {
        StepType = stepType;
        StepOrder = stepOrder;
        MaxAttempts =
            maxAttempts > 0
                ? maxAttempts
                : throw new ArgumentException("MaxAttempts must be positive.", nameof(maxAttempts));
        TimeoutSeconds =
            timeoutSeconds > 0
                ? timeoutSeconds
                : throw new ArgumentException("TimeoutSeconds must be positive.", nameof(timeoutSeconds));
        IsPolling = isPolling;
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Name must not be empty.", nameof(name));
    }

    private ExecutionPipelineStep() { }

    public string Name { get; private set; } = null!;
    public ExecutionPipelineStepType StepType { get; private set; }
    public int StepOrder { get; private set; }
    public int MaxAttempts { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public bool IsPolling { get; private set; }
}