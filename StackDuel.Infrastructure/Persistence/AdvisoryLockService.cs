using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
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

    public async Task<T> RunExclusiveAsync<T>(
        string lockNamespace,
        Guid key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({lockNamespace}), hashtext({key.ToString()}))",
            cancellationToken
        );

        T result = await action(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}