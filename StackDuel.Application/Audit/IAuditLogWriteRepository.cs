using StackDuel.Domain.Audit.Entities;

namespace StackDuel.Application.Audit;

public interface IAuditLogWriteRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}