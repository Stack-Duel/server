using Ardalis.Result;
using MediatR;
using StackDuel.Application.Audit;
using StackDuel.Domain.Audit.Entities;
using System.Text.Json;

namespace StackDuel.Application.Behaviors;

internal sealed class AuditBehavior<TRequest, TResponse>(
    IAuditLogWriteRepository auditLogWriteRepository,
    UserContext userContext
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        TResponse response = await next();

        if (request is IAuditableCommand auditable && response is IResult result && result.Status == ResultStatus.Ok)
        {
            string? detailsJson = auditable.AuditDetails is null
                ? null
                : JsonSerializer.Serialize(auditable.AuditDetails);

            AuditLogEntry entry = AuditLogEntry.Create(
                userContext.User?.Id,
                userContext.User?.Username ?? "unknown",
                auditable.AuditAction,
                auditable.AuditTargetType,
                auditable.AuditTargetId,
                detailsJson
            );

            await auditLogWriteRepository.AddAsync(entry, cancellationToken);
        }

        return response;
    }
}