using StackDuel.Application.Audit;
using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.AuditLog;

internal sealed class AuditLogReadRepository(StackDuelDbContext context) : IAuditLogReadRepository
{
    public async Task<PageResult<AuditLogEntryDto>> GetPageableAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        var query = context.AuditLogEntries.AsNoTracking();

        int total = await query.CountAsync(cancellationToken);

        List<AuditLogEntryDto> results = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip(offset)
            .Take(pagination.Size)
            .Select(a => new AuditLogEntryDto(
                a.Id,
                a.ActorUserId,
                a.ActorUsername,
                a.Action,
                a.TargetType,
                a.TargetId,
                a.DetailsJson,
                a.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PageResult<AuditLogEntryDto>
        {
            Results = results,
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }
}