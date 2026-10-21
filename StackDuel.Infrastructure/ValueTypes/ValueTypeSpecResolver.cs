using StackDuel.Application.ValueTypes;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Infrastructure.ValueTypes;

internal sealed class ValueTypeSpecResolver(IValueTypeDefinitionRepository repository) : IValueTypeSpecResolver
{
    public async Task<IReadOnlyDictionary<string, ValueTypeSpec>> ResolveAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default
    )
    {
        HashSet<string> requested = [.. keys];
        if (requested.Count == 0)
            return new Dictionary<string, ValueTypeSpec>();

        IReadOnlyList<ValueTypeDefinition> definitions = await repository.GetAllOrderedAsync(cancellationToken);

        return definitions
            .Where(d => requested.Contains(d.Key.Value))
            .ToDictionary(d => d.Key.Value, d => d.Spec);
    }
}