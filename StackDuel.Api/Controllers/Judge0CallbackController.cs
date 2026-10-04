using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.RateLimiting;
using StackDuel.Application.Services.Submissions;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/judge0")]
[AllowAnonymous]
[EnableRateLimiting(WellKnownPolicies.Judge0Callback)]
public sealed class Judge0CallbackController(ISubmissionService submissionService) : ControllerBase
{
    [HttpPut("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Unit>> ReceiveCallback(
        [FromQuery(Name = "key")] string? key,
        [FromQuery] Guid submissionId,
        CancellationToken cancellationToken
    ) => this.ToActionResult(await submissionService.ReceiveJudge0CallbackAsync(key, submissionId, cancellationToken));
}