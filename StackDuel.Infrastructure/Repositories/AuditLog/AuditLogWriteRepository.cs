using StackDuel.Application.Audit;
using StackDuel.Domain.Audit.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.AuditLog;

internal sealed class AuditLogWriteRepository(StackDuelDbContext context) : IAuditLogWriteRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        await context.AuditLogEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}