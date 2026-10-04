using StackDuel.Domain.ExecutionPipelines.Entities;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ExecutionPipelines;

public sealed class ExecutionPipeline : AggregateRoot
{
    public ExecutionPipeline(string name, string? description = null)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Name must not be empty.", nameof(name));
        Description = description;
    }

    public ExecutionPipelineStep AddStep(
        ExecutionPipelineStepType stepType,
        int stepOrder,
        int maxAttempts,
        int timeoutSeconds,
        bool isPolling,
        string name
    )
    {
        var step = new ExecutionPipelineStep(stepType, stepOrder, maxAttempts, timeoutSeconds, isPolling, name);
        _steps.Add(step);
        return step;
    }

    public ExecutionPipelineStep? FirstStep() => _steps.OrderBy(s => s.StepOrder).FirstOrDefault();

    public ExecutionPipelineStep? NextStep(int currentStepOrder) =>
        _steps.Where(s => s.StepOrder > currentStepOrder).OrderBy(s => s.StepOrder).FirstOrDefault();

    private ExecutionPipeline() { }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public IReadOnlyCollection<ExecutionPipelineStep> Steps => _steps.AsReadOnly();

    private readonly List<ExecutionPipelineStep> _steps = [];
}