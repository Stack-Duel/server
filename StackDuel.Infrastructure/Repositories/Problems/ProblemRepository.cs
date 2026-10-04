using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemRepository(StackDuelDbContext context) : IProblemRepository
{
    public async Task AddAsync(Problem entity, CancellationToken cancellationToken = default)
    {
        await context.Problems.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Problem entity, CancellationToken cancellationToken = default)
    {
        if (context.Entry(entity).State == EntityState.Detached)
            context.Problems.Update(entity);

        await NewChildEntityTracker.MarkNewAsAddedAsync(context, entity.History, cancellationToken);
        await NewChildEntityTracker.MarkNewAsAddedAsync(context, entity.Setups, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Problem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Include(p => p.Tags)
            .Include(p => p.Setups)
                .ThenInclude(s => s.TestSuites)
                    .ThenInclude(ts => ts.TestCases)
                        .ThenInclude(tc => tc.Inputs.OrderBy(i => i.Position))
            .Include(p => p.Setups)
                .ThenInclude(s => s.TestSuites)
                    .ThenInclude(ts => ts.TestCases)
                        .ThenInclude(tc => tc.ExpectedOutputs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Problem?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Include(p => p.Setups)
                .ThenInclude(s => s.TestSuites)
                    .ThenInclude(ts => ts.TestCases)
                        .ThenInclude(tc => tc.Inputs.OrderBy(i => i.Position))
            .Include(p => p.Setups)
                .ThenInclude(s => s.TestSuites)
                    .ThenInclude(ts => ts.TestCases)
                        .ThenInclude(tc => tc.ExpectedOutputs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Slug.Value == slug.Value, cancellationToken);

    public async Task<Problem?> FindBySetupIdAsync(Guid setupId, CancellationToken cancellationToken = default) =>
        await context
            .Problems.IgnoreQueryFilters()
            .Include(p => p.Setups)
                .ThenInclude(s => s.TestSuites)
                    .ThenInclude(ts => ts.TestCases)
                        .ThenInclude(tc => tc.Inputs.OrderBy(i => i.Position))
            .Include(p => p.Setups)
                .ThenInclude(s => s.TestSuites)
                    .ThenInclude(ts => ts.TestCases)
                        .ThenInclude(tc => tc.ExpectedOutputs)
            .Where(p => p.Setups.Any(s => s.Id == setupId))
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProblemTag>> FindOrCreateTagsAsync(
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default
    )
    {
        if (tagNames.Count == 0)
            return [];

        // Filtered in-memory rather than via a `.Name.Value` predicate — member access on a
        // HasConversion-mapped property is unreliable to translate outside a terminal Select
        // (see the OrderBy(g => g.Name.Value) failure fixed in GroupReadRepository). The tags
        // table is small, so loading it fully here is cheap.
        List<ProblemTag> allTags = await context.Tags.ToListAsync(cancellationToken);
        List<ProblemTag> existing = [.. allTags.Where(t => tagNames.Contains(t.Name.Value))];

        HashSet<string> existingNames = [.. existing.Select(t => t.Name.Value)];

        List<ProblemTag> created = [];
        foreach (string name in tagNames.Where(n => !existingNames.Contains(n)))
        {
            var tag = new ProblemTag(new Tag(name));
            context.Tags.Add(tag);
            created.Add(tag);
        }

        return [.. existing, .. created];
    }
}