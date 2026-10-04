using StackDuel.Application.ExecutionEngine;
using StackDuel.Domain.ExecutionPipelines.Enums;

namespace StackDuel.Infrastructure.ExecutionEngine.StepHandlers;

internal sealed class StepHandlerRegistry(IEnumerable<IStepHandler> handlers) : IStepHandlerRegistry
{
    private readonly IReadOnlyList<IStepHandler> _handlers = [.. handlers];

    public IStepHandler Resolve(ExecutionPipelineStepType stepType) =>
        _handlers.FirstOrDefault(h => h.CanHandle(stepType))
        ?? throw new InvalidOperationException($"No step handler registered for step type '{stepType}'.");
}