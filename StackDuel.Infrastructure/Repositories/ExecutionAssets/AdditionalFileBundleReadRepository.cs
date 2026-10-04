using Microsoft.EntityFrameworkCore;
using StackDuel.Application.ExecutionAssets;
using StackDuel.Domain.ExecutionAssets.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.ExecutionAssets;

internal sealed class AdditionalFileBundleReadRepository(StackDuelDbContext context)
    : IAdditionalFileBundleReadRepository
{
    public async Task<AdditionalFileBundle?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await context.AdditionalFileBundles.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
}