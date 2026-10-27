using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class AdvisoryLockService(StackDuelDbContext context) : IAdvisoryLockService
{
    private const int UserScopedLockNamespace = 837264;

    public async Task AcquireUserScopedLockAsync(Guid key, CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({UserScopedLockNamespace}, hashtext({key.ToString()}))",
            cancellationToken
        );
    }
}