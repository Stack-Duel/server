using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.FeatureFlags;
using StackDuel.Application;
using StackDuel.Application.FeatureFlags.Dtos;
using StackDuel.Application.Services.FeatureFlags;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/feature-flag")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class FeatureFlagController(UserContext userContext, IFeatureFlagAdminService adminService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyDictionary<string, bool>> GetFlags() => Ok(userContext.Flags);

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FeatureFlagAdminDto>>> GetAdminFlags(
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.GetAllAsync(cancellationToken));

    [HttpGet("admin/{key}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureFlagAdminDto>> GetAdminFlag(
        string key,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.GetByKeyAsync(key, cancellationToken));

    [HttpPost("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> CreateFlag(
        [FromBody] CreateFeatureFlagRequest request,
        CancellationToken cancellationToken
    ) =>
        this.ToActionResult(
            await adminService.CreateAsync(
                request.Key,
                request.Name,
                request.Description,
                request.DefaultEnabled,
                cancellationToken
            )
        );

    [HttpPut("admin/{id:guid}/default")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateDefault(
        Guid id,
        [FromBody] UpdateFeatureFlagDefaultRequest request,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.UpdateDefaultAsync(id, request.DefaultEnabled, cancellationToken));

    [HttpPut("admin/{id:guid}/rollout")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateRollout(
        Guid id,
        [FromBody] UpdateFeatureFlagRolloutRequest request,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.UpdateRolloutAsync(id, request.RolloutPercentage, cancellationToken));

    [HttpPut("admin/{id:guid}/user-overrides/{userId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetUserOverride(
        Guid id,
        Guid userId,
        [FromBody] SetFeatureFlagOverrideRequest request,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.SetUserOverrideAsync(id, userId, request.Effect, cancellationToken));

    [HttpDelete("admin/{id:guid}/user-overrides/{userId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveUserOverride(Guid id, Guid userId, CancellationToken cancellationToken) =>
        this.ToActionResult(await adminService.RemoveUserOverrideAsync(id, userId, cancellationToken));

    [HttpPut("admin/{id:guid}/group-overrides/{groupId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetGroupOverride(
        Guid id,
        Guid groupId,
        [FromBody] SetFeatureFlagOverrideRequest request,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await adminService.SetGroupOverrideAsync(id, groupId, request.Effect, cancellationToken));

    [HttpDelete("admin/{id:guid}/group-overrides/{groupId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageFeatureFlags)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveGroupOverride(Guid id, Guid groupId, CancellationToken cancellationToken) =>
        this.ToActionResult(await adminService.RemoveGroupOverrideAsync(id, groupId, cancellationToken));
}