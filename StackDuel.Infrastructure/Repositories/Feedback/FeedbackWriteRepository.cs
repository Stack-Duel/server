using StackDuel.Domain.Feedback;
using StackDuel.Domain.Feedback.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Feedback;

internal sealed class FeedbackWriteRepository(StackDuelDbContext context) : IFeedbackWriteRepository
{
    public async Task AddAsync(FeedbackSubmission entity, CancellationToken cancellationToken = default)
    {
        await context.FeedbackSubmissions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeedbackSubmission?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.FeedbackSubmissions.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(FeedbackSubmission entity, CancellationToken cancellationToken = default)
    {
        context.FeedbackSubmissions.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}