using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Application;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Services.Dashboard;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class DashboardController(IDashboardService dashboardService, UserContext userContext) : ControllerBase
{
    private const int NewUsersDays = 30;

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminDashboard)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminDashboardStatsDto>> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await dashboardService.GetAdminDashboardStatsAsync(NewUsersDays, cancellationToken));
    }

    [HttpGet("me")]
    [RequireUser]
    [ProducesResponseType(typeof(UserDashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDashboardStatsDto>> GetMyDashboardStats(CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<UserDashboardStatsDto>.Unauthorized());

        return this.ToActionResult(
            await dashboardService.GetMyDashboardStatsAsync(userContext.User.Id, cancellationToken)
        );
    }
}