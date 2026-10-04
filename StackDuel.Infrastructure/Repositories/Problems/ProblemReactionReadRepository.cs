using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemReactionReadRepository(StackDuelDbContext context) : IProblemReactionReadRepository
{
    public async Task<ProblemReactionType?> FindReactionTypeByKeyAsync(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        string normalizedKey = key.Trim().ToLowerInvariant();

        return await context
            .ProblemReactionTypes.AsNoTracking()
            .FirstOrDefaultAsync(type => type.Key == normalizedKey, cancellationToken);
    }

    public async Task<ProblemReactionSummaryDto> GetSummaryAsync(
        Guid problemId,
        Guid? userId,
        CancellationToken cancellationToken = default
    )
    {
        var counts = await context
            .ProblemReactions.AsNoTracking()
            .Where(reaction => reaction.ProblemId == problemId && reaction.IsActive)
            .GroupBy(reaction => reaction.ReactionTypeId)
            .Select(group => new { ReactionTypeId = group.Key, Count = group.Count() })
            .Join(
                context.ProblemReactionTypes.AsNoTracking(),
                group => group.ReactionTypeId,
                type => type.Id,
                (group, type) =>
                    new
                    {
                        type.Key,
                        type.Name,
                        type.Emoji,
                        type.SortOrder,
                        group.Count,
                    }
            )
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        string? currentUserReactionKey = null;
        if (userId is Guid uid)
        {
            currentUserReactionKey = await context
                .ProblemReactions.AsNoTracking()
                .Where(reaction => reaction.ProblemId == problemId && reaction.UserId == uid && reaction.IsActive)
                .Join(
                    context.ProblemReactionTypes.AsNoTracking(),
                    reaction => reaction.ReactionTypeId,
                    type => type.Id,
                    (reaction, type) => type.Key
                )
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ProblemReactionSummaryDto(
            counts.Select(x => new ProblemReactionCountDto(x.Key, x.Name, x.Emoji, x.Count)).ToList(),
            currentUserReactionKey
        );
    }
}