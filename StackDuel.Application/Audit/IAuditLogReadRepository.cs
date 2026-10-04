using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;

namespace StackDuel.Application.Audit;

public interface IAuditLogReadRepository
{
    Task<PageResult<AuditLogEntryDto>> GetPageableAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default
    );
}