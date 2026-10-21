using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class ValueTypeDefinitionSeeder(StackDuelDbContext context) : IStaticSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<string> existingKeys = await context
            .ValueTypeDefinitions.Select(x => x.Key.Value)
            .ToListAsync(cancellationToken);

        HashSet<string> existingKeySet = [.. existingKeys];

        foreach (WellKnownValueTypes.BuiltInValueType builtIn in WellKnownValueTypes.All)
        {
            if (existingKeySet.Contains(builtIn.Key))
                continue;

            var definition = new ValueTypeDefinition(
                new ValueTypeKey(builtIn.Key),
                builtIn.Name,
                builtIn.Description,
                builtIn.Spec,
                isBuiltIn: true
            );

            context.ValueTypeDefinitions.Add(definition);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}