using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Leaderboard;
using StackDuel.Application;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Leaderboards;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class LeaderboardController(ILeaderboardService leaderboardService, UserContext userContext)
    : ControllerBase
{
    [HttpGet]
    [RequireFeature(WellKnownFeatures.Leaderboards)]
    [ProducesResponseType(typeof(PageResult<LeaderboardEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResult<LeaderboardEntryDto>>> GetLeaderboard(
        [FromQuery] GetLeaderboardRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await leaderboardService.GetLeaderboardAsync(
                query.GameModeKey,
                query.TimeLimitInSeconds,
                new PaginationRequest { Page = query.Page, Size = query.Size },
                userContext.User?.Id,
                cancellationToken
            )
        );
    }

    [HttpGet("me")]
    [RequireUser]
    [RequireFeature(WellKnownFeatures.Leaderboards)]
    [ProducesResponseType(typeof(MyLeaderboardEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyLeaderboardEntryDto>> GetMyLeaderboardEntry(
        [FromQuery] GetMyLeaderboardEntryRequest query,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<MyLeaderboardEntryDto>.Unauthorized());

        return this.ToActionResult(
            await leaderboardService.GetMyEntryAsync(
                query.GameModeKey,
                query.TimeLimitInSeconds,
                userContext.User.Id,
                cancellationToken
            )
        );
    }
}