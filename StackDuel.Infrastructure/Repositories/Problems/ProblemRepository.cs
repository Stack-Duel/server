using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemRepository(StackDuelDbContext context) : IProblemRepository
{
    private const string InsertTagsOnConflictDoNothing = """
        INSERT INTO tags (id, name)
        SELECT gen_random_uuid(), unnest(@names)
        ON CONFLICT (name) DO NOTHING
        """;

    public async Task AddAsync(Problem entity, CancellationToken cancellationToken = default)
    {
        await context.Problems.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Problem entity, CancellationToken cancellationToken = default)
    {
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
        IEnumerable<Tag> tags,
        CancellationToken cancellationToken = default
    )
    {
        List<Tag> tagList = [.. tags];

        if (tagList.Count == 0)
            return [];

        string[] names = [.. tagList.Select(t => t.Value)];

        await context.Database.ExecuteSqlRawAsync(
            InsertTagsOnConflictDoNothing,
            [new NpgsqlParameter("names", names)],
            cancellationToken
        );

        return await context.Tags.Where(t => tagList.Contains(t.Name)).ToListAsync(cancellationToken);
    }
}