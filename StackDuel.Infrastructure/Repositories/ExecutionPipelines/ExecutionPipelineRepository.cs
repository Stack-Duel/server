using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.ExecutionPipelines;

internal sealed class ExecutionPipelineRepository(StackDuelDbContext context) : IExecutionPipelineRepository
{
    public async Task AddAsync(ExecutionPipeline entity, CancellationToken cancellationToken = default)
    {
        await context.ExecutionPipelines.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExecutionPipeline?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.ExecutionPipelines.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<ExecutionPipeline?> FindByIdWithStepsAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await context.ExecutionPipelines.Include(p => p.Steps).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Guid?> FindIdByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await context
            .ExecutionPipelines.Where(p => p.Name == name)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpdateAsync(ExecutionPipeline entity, CancellationToken cancellationToken = default)
    {
        context.ExecutionPipelines.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}