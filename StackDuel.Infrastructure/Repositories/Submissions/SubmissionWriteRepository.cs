using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Submissions;

internal sealed class SubmissionWriteRepository(StackDuelDbContext context) : ISubmissionWriteRepository
{
    public async Task AddAsync(Submission entity, CancellationToken cancellationToken = default)
    {
        await context.Submissions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Submission?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context
            .Submissions.Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Submission entity, CancellationToken cancellationToken = default)
    {
        context.Submissions.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}