using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Infrastructure.Persistence;

internal static class NewChildEntityTracker
{
    public static async Task MarkNewAsAddedAsync<TChild>(
        StackDuelDbContext context,
        IEnumerable<TChild> children,
        CancellationToken cancellationToken
    )
        where TChild : Entity
    {
        List<TChild> childList = [.. children];

        if (childList.Count == 0)
            return;

        HashSet<Guid> childIds = [.. childList.Select(c => c.Id)];

        HashSet<Guid> existingIds =
        [
            .. await context
                .Set<TChild>()
                .AsNoTracking()
                .Where(c => childIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken),
        ];

        foreach (TChild child in childList.Where(c => !existingIds.Contains(c.Id)))
            context.Entry(child).State = EntityState.Added;
    }
}