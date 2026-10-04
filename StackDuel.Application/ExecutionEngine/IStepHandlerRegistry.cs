using StackDuel.Domain.ExecutionPipelines.Enums;

namespace StackDuel.Application.ExecutionEngine;

public interface IStepHandlerRegistry
{
    IStepHandler Resolve(ExecutionPipelineStepType stepType);
}