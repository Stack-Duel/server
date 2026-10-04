using StackDuel.Domain.ExecutionAssets.Entities;

namespace StackDuel.Application.ExecutionAssets;

public interface IAdditionalFileBundleReadRepository
{
    Task<AdditionalFileBundle?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}