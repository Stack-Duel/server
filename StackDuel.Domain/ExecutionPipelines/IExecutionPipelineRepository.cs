using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ExecutionPipelines;

public interface IExecutionPipelineRepository : IRepository<ExecutionPipeline>
{
    Task<ExecutionPipeline?> FindByIdWithStepsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> FindIdByNameAsync(string name, CancellationToken cancellationToken = default);
}