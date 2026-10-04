using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Feedback;
using StackDuel.Application;
using StackDuel.Application.Feedback.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Feedback;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class FeedbackController(IFeedbackService feedbackService, UserContext userContext) : ControllerBase
{
    [HttpPost]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateFeedback)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> SubmitFeedback(
        [FromBody] SubmitFeedbackRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<Guid>.Unauthorized());

        string? pageUrl = Request.Headers.Referer.Count > 0 ? Request.Headers.Referer.ToString() : null;
        string? userAgent = Request.Headers.UserAgent.Count > 0 ? Request.Headers.UserAgent.ToString() : null;

        return this.ToActionResult(
            await feedbackService.SubmitFeedbackAsync(
                new SubmitFeedbackDto(
                    userContext.User.Id,
                    request.Type,
                    request.Message,
                    request.Rating,
                    request.ContextType,
                    request.ContextEntityId,
                    pageUrl,
                    userAgent
                ),
                cancellationToken
            )
        );
    }

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminFeedback)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PageResult<AdminFeedbackListItemDto>>> GetAdminFeedback(
        [FromQuery] GetAdminFeedbackPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await feedbackService.GetAdminFeedbackPageableAsync(
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                query.Type,
                query.Status,
                cancellationToken
            )
        );
    }

    [HttpGet("admin/{feedbackId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminFeedback)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminFeedbackDetailDto>> GetAdminFeedbackDetail(
        Guid feedbackId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await feedbackService.GetAdminFeedbackDetailAsync(feedbackId, cancellationToken));
    }

    [HttpPatch("admin/{feedbackId:guid}/status")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.UpdateAdminFeedback)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateFeedbackStatus(
        Guid feedbackId,
        [FromBody] UpdateFeedbackStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await feedbackService.UpdateFeedbackStatusAsync(
                feedbackId,
                request.Status,
                request.AdminNote,
                cancellationToken
            )
        );
    }
}