using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.DailyChallenge;
using StackDuel.Application;
using StackDuel.Application.DailyChallenges.Dtos;
using StackDuel.Application.Services.DailyChallenges;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class DailyChallengeController(IDailyChallengeService dailyChallengeService, UserContext userContext)
    : ControllerBase
{
    [HttpGet("today")]
    [RequireUser]
    [ProducesResponseType(typeof(TodaysDailyChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodaysDailyChallengeDto>> GetTodaysChallenge(CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<TodaysDailyChallengeDto>.Unauthorized());

        return this.ToActionResult(
            await dailyChallengeService.GetTodaysChallengeAsync(userContext.User.Id, cancellationToken)
        );
    }

    [HttpGet("admin/upcoming")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UpcomingDailyChallengeDto>>> GetUpcomingChallenges(
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await dailyChallengeService.GetUpcomingChallengesAsync(cancellationToken));
    }

    [HttpPut("admin/{date}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.UpdateAdminProblems)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> UpdateChallenge(
        DateOnly date,
        [FromBody] UpdateDailyChallengeRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await dailyChallengeService.UpdateChallengeAsync(date, request.ProblemId, cancellationToken)
        );
    }
}