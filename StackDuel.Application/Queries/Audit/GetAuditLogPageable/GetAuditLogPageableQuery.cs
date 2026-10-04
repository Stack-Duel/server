using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;

namespace StackDuel.Application.Queries.Audit.GetAuditLogPageable;

public sealed record GetAuditLogPageableQuery(PaginationRequest PaginationRequest)
    : IQuery<PageResult<AuditLogEntryDto>>;