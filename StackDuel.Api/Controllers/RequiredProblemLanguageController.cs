using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Problem.RequiredLanguages;
using StackDuel.Application.Problems.RequiredLanguages.Dtos;
using StackDuel.Application.Services.Problems;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/problem-required-language")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class RequiredProblemLanguageController(IRequiredProblemLanguageAdminService adminService)
    : ControllerBase
{
    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageRequiredProblemLanguages)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<RequiredProblemLanguageAdminDto>>> GetAll(
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.GetAllAsync(cancellationToken));

    [HttpPost("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageRequiredProblemLanguages)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> Add(
        [FromBody] AddRequiredProblemLanguageRequest request,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.AddAsync(request.LanguageVersionId, cancellationToken));

    [HttpDelete("admin/{id:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageRequiredProblemLanguages)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await adminService.RemoveAsync(id, cancellationToken));
}