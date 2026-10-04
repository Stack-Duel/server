using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Problem;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Services.Problems;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
[RequireUser]
[RequirePermission(WellKnownPermissions.ReadAdminProblems)]
public sealed class ProblemPoolController(IProblemPoolService problemPoolService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ProblemPoolDto>>> GetPools(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await problemPoolService.GetProblemPoolsAsync(cancellationToken));
    }

    [HttpGet("{poolKey}/problems")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResult<AdminProblemListItemDto>>> GetPoolMembers(
        string poolKey,
        [FromQuery] GetProblemPoolMembersPagedRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.GetProblemPoolMembersPagedAsync(
                poolKey,
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

    [HttpGet("{poolKey}/problems/ids")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetPoolMemberIds(
        string poolKey,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await problemPoolService.GetProblemPoolMemberIdsAsync(poolKey, cancellationToken));
    }

    [HttpGet("{poolKey}/problems/ordered")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdminProblemListRowDto>>> GetOrderedPoolMembers(
        string poolKey,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.GetOrderedProblemPoolMembersAsync(poolKey, cancellationToken)
        );
    }

    [HttpPost]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> CreatePool(
        [FromBody] CreateProblemPoolRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.CreateProblemPoolAsync(
                request.Key,
                request.Name,
                request.Description,
                cancellationToken
            )
        );
    }

    [HttpPut("{poolKey}/problems/{problemId:guid}")]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> AddProblem(
        string poolKey,
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.AddProblemToPoolAsync(poolKey, problemId, cancellationToken)
        );
    }

    [HttpPost("{poolKey}/problems/bulk")]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> AddProblems(
        string poolKey,
        [FromBody] AddProblemsToPoolRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.AddProblemsToPoolAsync(
                poolKey,
                request.ProblemIds,
                request.SelectAllMatching,
                request.Search,
                request.ExcludedProblemIds,
                cancellationToken
            )
        );
    }

    [HttpPut("{poolKey}/problems/reorder")]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> ReorderPool(
        string poolKey,
        [FromBody] ReorderProblemPoolRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.ReorderProblemPoolAsync(poolKey, request.ProblemIds, cancellationToken)
        );
    }

    [HttpDelete("{poolKey}/problems/{problemId:guid}")]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> RemoveProblem(
        string poolKey,
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await problemPoolService.RemoveProblemFromPoolAsync(poolKey, problemId, cancellationToken)
        );
    }
}