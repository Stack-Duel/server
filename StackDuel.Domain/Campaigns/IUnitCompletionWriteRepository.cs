using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Domain.Campaigns;

public interface IUnitCompletionWriteRepository
{
    Task AddAsync(UnitCompletion entity, CancellationToken cancellationToken = default);
}