using StackDuel.Domain.SeedWork;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.ValueTypes;

public interface IValueTypeDefinitionRepository : IRepository<ValueTypeDefinition>
{
    Task<IReadOnlyList<ValueTypeDefinition>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    Task<ValueTypeDefinition?> FindByKeyAsync(ValueTypeKey key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> FindExistingKeysAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default
    );

    Task RemoveAsync(ValueTypeDefinition definition, CancellationToken cancellationToken = default);
}