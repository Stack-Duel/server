using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Application.ValueTypes;

public interface IValueTypeSpecResolver
{
    Task<IReadOnlyDictionary<string, ValueTypeSpec>> ResolveAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default
    );
}