using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.ValueTypes;
using StackDuel.Application.Services.ValueTypes;
using StackDuel.Application.ValueTypes.Dtos;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class ValueTypeController(IValueTypeService valueTypeService) : ControllerBase
{
    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageValueTypes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ValueTypeDefinitionDto>>> GetAll(CancellationToken cancellationToken) =>
        this.ToActionResult(await valueTypeService.GetDefinitionsAsync(cancellationToken));

    [HttpGet("admin/metadata")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageValueTypes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ValueTypeMetadataDto>> GetMetadata(CancellationToken cancellationToken) =>
        this.ToActionResult(await valueTypeService.GetMetadataAsync(cancellationToken));

    [HttpPost("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageValueTypes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateValueTypeDefinitionRequest request,
        CancellationToken cancellationToken
    ) =>
        this.ToActionResult(
            await valueTypeService.CreateDefinitionAsync(
                request.Key,
                request.Name,
                request.Description,
                ToDto(request.Spec),
                cancellationToken
            )
        );

    [HttpPut("admin/{id:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageValueTypes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] UpdateValueTypeDefinitionRequest request,
        CancellationToken cancellationToken
    ) =>
        this.ToActionResult(
            await valueTypeService.UpdateDefinitionAsync(
                id,
                request.Name,
                request.Description,
                request.Spec is null ? null : ToDto(request.Spec),
                cancellationToken
            )
        );

    [HttpDelete("admin/{id:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageValueTypes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await valueTypeService.DeleteDefinitionAsync(id, cancellationToken));

    private static ValueTypeSpecDto ToDto(ValueTypeSpecRequest request) =>
        new(
            request.Kind,
            request.Min,
            request.Max,
            request.LengthMin,
            request.LengthMax,
            request.Charset,
            request.Element is null ? null : ToDto(request.Element)
        );
}