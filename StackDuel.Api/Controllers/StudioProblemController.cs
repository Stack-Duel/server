using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.RateLimiting;
using StackDuel.Application;
using StackDuel.Application.StudioProblems.Dtos;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/studio/problems")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class StudioProblemController(IStudioProblemService studioProblemService, UserContext userContext)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudioProblemDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<StudioProblemDetailsDto>.Unauthorized());

        return this.ToActionResult(
            await studioProblemService.GetStudioProblemByIdAsync(id, userContext.User.Id, cancellationToken)
        );
    }

    [HttpGet("{id:guid}/setups")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StudioProblemDetailsSetupDto>>> GetSetupsAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<IEnumerable<StudioProblemDetailsSetupDto>>.Unauthorized());

        return this.ToActionResult(
            await studioProblemService.GetStudioProblemDetailSetupsAsync(id, userContext.User.Id, cancellationToken)
        );
    }
}
