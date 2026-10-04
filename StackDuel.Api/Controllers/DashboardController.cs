using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Services.Dashboard;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
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
}