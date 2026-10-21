using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.ValueTypes;

internal sealed class ValueTypeDefinitionRepository(StackDuelDbContext context) : IValueTypeDefinitionRepository
{
    public async Task AddAsync(ValueTypeDefinition entity, CancellationToken cancellationToken = default)
    {
        await context.ValueTypeDefinitions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ValueTypeDefinition entity, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValueTypeDefinition?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.ValueTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ValueTypeDefinition>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default
    ) =>
        await context
            .ValueTypeDefinitions.OrderByDescending(x => x.IsBuiltIn)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);

    public async Task<ValueTypeDefinition?> FindByKeyAsync(
        ValueTypeKey key,
        CancellationToken cancellationToken = default
    ) => await context.ValueTypeDefinitions.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

    public async Task<IReadOnlyList<string>> FindExistingKeysAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default
    )
    {
        List<ValueTypeKey> candidates = [.. keys.Select(key => new ValueTypeKey(key))];

        return await context
            .ValueTypeDefinitions.Where(x => candidates.Contains(x.Key))
            .Select(x => x.Key.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveAsync(ValueTypeDefinition definition, CancellationToken cancellationToken = default)
    {
        context.ValueTypeDefinitions.Remove(definition);
        await context.SaveChangesAsync(cancellationToken);
    }
}