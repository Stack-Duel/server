using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class PostgresAdvisoryLock(StackDuelDbContext context) : IAdvisoryLock
{
    public async Task<T> RunExclusiveAsync<T>(
        string lockNamespace,
        Guid key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        NpgsqlParameter[] parameters =
        [
            new("lock_namespace", NpgsqlDbType.Varchar) { Value = lockNamespace },
            new("lock_key", NpgsqlDbType.Varchar) { Value = key.ToString() },
        ];
        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtext(@lock_namespace), hashtext(@lock_key))",
            parameters,
            cancellationToken
        );

        T result = await action(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}