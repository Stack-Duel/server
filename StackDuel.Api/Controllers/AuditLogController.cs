using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Audit;
using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Audit;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/audit-log")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class AuditLogController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminAuditLog)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PageResult<AuditLogEntryDto>>> GetAuditLog(
        [FromQuery] GetAuditLogPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await auditLogService.GetPageableAsync(
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                cancellationToken
            )
        );
    }
}