namespace StackDuel.Domain.ValueTypes;

public interface IValueTypeUsageRepository
{
    Task<bool> IsKeyInUseAsync(string key, CancellationToken cancellationToken = default);
}