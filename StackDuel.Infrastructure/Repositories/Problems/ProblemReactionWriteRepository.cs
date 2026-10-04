using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemReactionWriteRepository(StackDuelDbContext context) : IProblemReactionWriteRepository
{
    public async Task<ProblemReaction?> FindByProblemUserAndTypeAsync(
        Guid problemId,
        Guid userId,
        Guid reactionTypeId,
        CancellationToken cancellationToken = default
    )
    {
        return await context.ProblemReactions.FirstOrDefaultAsync(
            reaction =>
                reaction.ProblemId == problemId
                && reaction.UserId == userId
                && reaction.ReactionTypeId == reactionTypeId,
            cancellationToken
        );
    }

    public async Task<ProblemReaction?> FindActiveByProblemAndUserAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await context.ProblemReactions.FirstOrDefaultAsync(
            reaction => reaction.ProblemId == problemId && reaction.UserId == userId && reaction.IsActive,
            cancellationToken
        );
    }

    public async Task AddAsync(ProblemReaction entity, CancellationToken cancellationToken = default)
    {
        await context.ProblemReactions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProblemReaction entity, CancellationToken cancellationToken = default)
    {
        context.ProblemReactions.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}