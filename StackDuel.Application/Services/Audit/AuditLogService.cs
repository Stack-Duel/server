using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Audit.GetAuditLogPageable;
using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Services.Audit;

public interface IAuditLogService
{
    Task<Result<PageResult<AuditLogEntryDto>>> GetPageableAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken
    );
}

internal sealed class AuditLogService(IMediator mediator) : IAuditLogService
{
    public async Task<Result<PageResult<AuditLogEntryDto>>> GetPageableAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetAuditLogPageableQuery(pagination), cancellationToken);
}