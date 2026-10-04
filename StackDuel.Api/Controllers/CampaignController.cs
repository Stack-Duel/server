using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Campaign;
using StackDuel.Application;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Campaigns;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
[RequireFeature(WellKnownFeatures.Campaigns)]
public sealed class CampaignController(ICampaignService campaignService, UserContext userContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CampaignSummaryDto>>> GetCampaignPath(
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await campaignService.GetCampaignPathAsync(cancellationToken));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignDto>> GetCampaignBySlug(string slug, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await campaignService.GetCampaignBySlugAsync(slug, cancellationToken));
    }

    [HttpGet("progress")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserCampaignProgressDto>>> GetMyEnrollments(
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(await campaignService.GetMyEnrollmentsAsync(userContext.User.Id, cancellationToken));
    }

    [HttpGet("stats")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserLearningStatsDto>> GetMyLearningStats(CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await campaignService.GetMyLearningStatsAsync(userContext.User.Id, cancellationToken)
        );
    }

    [HttpGet("{campaignId:guid}/progress")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCampaignProgressDto>> GetMyCampaignProgress(
        Guid campaignId,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await campaignService.GetMyCampaignProgressAsync(campaignId, userContext.User.Id, cancellationToken)
        );
    }

    [HttpPost("{campaignId:guid}/enroll")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> EnrollInCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await campaignService.EnrollInCampaignAsync(campaignId, userContext.User.Id, cancellationToken)
        );
    }

    [HttpPost("units/{unitId:guid}/complete")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> CompleteUnit(Guid unitId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await campaignService.CompleteUnitAsync(unitId, userContext.User.Id, cancellationToken)
        );
    }

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PageResult<AdminCampaignListItemDto>>> GetAdminCampaigns(
        [FromQuery] GetAdminCampaignsPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.GetAdminCampaignsPageableAsync(
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                query.Search,
                cancellationToken
            )
        );
    }

    [HttpGet("admin/{campaignId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignDto>> GetAdminCampaignDetail(
        Guid campaignId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await campaignService.GetAdminCampaignDetailAsync(campaignId, cancellationToken));
    }

    [HttpPost("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> CreateCampaign(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.CreateCampaignAsync(
                request.Title,
                request.Description,
                request.Difficulty,
                cancellationToken
            )
        );
    }

    [HttpPut("admin/{campaignId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> UpdateCampaignDetails(
        Guid campaignId,
        [FromBody] UpdateCampaignDetailsRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.UpdateCampaignDetailsAsync(
                campaignId,
                request.Title,
                request.Description,
                request.Difficulty,
                request.IconKey,
                cancellationToken
            )
        );
    }

    [HttpPost("admin/{campaignId:guid}/publish")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> PublishCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await campaignService.PublishCampaignAsync(campaignId, cancellationToken));
    }

    [HttpPost("admin/{campaignId:guid}/archive")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> ArchiveCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await campaignService.ArchiveCampaignAsync(campaignId, cancellationToken));
    }

    [HttpPut("admin/{campaignId:guid}/prerequisites")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> SetCampaignPrerequisites(
        Guid campaignId,
        [FromBody] SetCampaignPrerequisitesRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.SetCampaignPrerequisitesAsync(
                campaignId,
                request.RequiredCampaignIds,
                cancellationToken
            )
        );
    }

    [HttpPost("admin/{campaignId:guid}/modules")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddCampaignModule(
        Guid campaignId,
        [FromBody] AddCampaignModuleRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.AddCampaignModuleAsync(
                campaignId,
                request.Title,
                request.Description,
                cancellationToken
            )
        );
    }

    [HttpPut("admin/{campaignId:guid}/modules/{moduleId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> UpdateCampaignModule(
        Guid campaignId,
        Guid moduleId,
        [FromBody] UpdateCampaignModuleRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.UpdateCampaignModuleAsync(
                campaignId,
                moduleId,
                request.Title,
                request.Description,
                cancellationToken
            )
        );
    }

    [HttpPost("admin/{campaignId:guid}/modules/{moduleId:guid}/units")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddCampaignUnit(
        Guid campaignId,
        Guid moduleId,
        [FromBody] AddCampaignUnitRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.AddCampaignUnitAsync(
                campaignId,
                moduleId,
                request.Title,
                request.Content,
                request.UnitType,
                request.EstimatedMinutes,
                cancellationToken
            )
        );
    }

    [HttpPut("admin/{campaignId:guid}/modules/{moduleId:guid}/units/{unitId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> UpdateCampaignUnit(
        Guid campaignId,
        Guid moduleId,
        Guid unitId,
        [FromBody] UpdateCampaignUnitRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.UpdateCampaignUnitAsync(
                campaignId,
                moduleId,
                unitId,
                request.Title,
                request.Content,
                request.UnitType,
                request.EstimatedMinutes,
                cancellationToken
            )
        );
    }

    [HttpPut("admin/{campaignId:guid}/modules/{moduleId:guid}/units/{unitId:guid}/problems")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ManageAdminCampaigns)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Unit>> SetUnitProblems(
        Guid campaignId,
        Guid moduleId,
        Guid unitId,
        [FromBody] SetUnitProblemsRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await campaignService.SetUnitProblemsAsync(
                campaignId,
                moduleId,
                unitId,
                request.ProblemIds,
                cancellationToken
            )
        );
    }
}