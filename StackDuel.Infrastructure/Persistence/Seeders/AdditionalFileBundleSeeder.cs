using StackDuel.Domain.ExecutionAssets.Entities;
using StackDuel.Domain.ExecutionAssets.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class AdditionalFileBundleSeeder(StackDuelDbContext context)
{
    public async Task<Guid> GetOrCreateAsync(string name, byte[] content, CancellationToken cancellationToken = default)
    {
        Guid? existingId = await context
            .AdditionalFileBundles.AsNoTracking()
            .Where(b => b.Name == new AdditionalFileBundleName(name))
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is not null)
            return existingId.Value;

        var bundle = new AdditionalFileBundle(new AdditionalFileBundleName(name), content);
        context.AdditionalFileBundles.Add(bundle);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        return bundle.Id;
    }
}