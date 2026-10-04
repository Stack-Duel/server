using StackDuel.Application.Audit;
using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Audit.GetAuditLogPageable;

internal sealed class GetAuditLogPageableHandler(IAuditLogReadRepository auditLogReadRepository)
    : IQueryHandler<GetAuditLogPageableQuery, PageResult<AuditLogEntryDto>>
{
    public async Task<Result<PageResult<AuditLogEntryDto>>> Handle(
        GetAuditLogPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await auditLogReadRepository.GetPageableAsync(request.PaginationRequest, cancellationToken);

        return Result.Success(result);
    }
}