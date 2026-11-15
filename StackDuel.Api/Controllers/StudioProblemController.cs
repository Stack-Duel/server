using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Application;
using StackDuel.Application.StudioProblems.Dtos;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/studio/problems")]
[EnableRateLimiting(WellKnownPolicies.General)]
[RequireUser]
[RequirePermission(WellKnownPermissions.ReadStudioProblems)]
public sealed class StudioProblemController(IStudioProblemService studioProblemService, UserContext userContext)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudioProblemDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(
            await studioProblemService.GetStudioProblemByIdAsync(id, userContext.User!.Id, cancellationToken)
        );
    }

    [HttpGet("initial-values")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InitialStudioProblemSetupDto>> GetInitialStudioProblemAsync(
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await studioProblemService.GetInitialStudioProblemSetupAsync(cancellationToken));
    }

    [HttpGet("{id:guid}/setups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StudioProblemDetailsSetupDto>>> GetSetupsAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await studioProblemService.GetStudioProblemDetailSetupsAsync(id, userContext.User!.Id, cancellationToken)
        );
    }
}
