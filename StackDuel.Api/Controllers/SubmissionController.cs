using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.Submission;
using StackDuel.Application;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Submissions;
using StackDuel.Application.Submissions.Dtos;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.Submissions)]
public sealed class SubmissionController(ISubmissionService submissionService, UserContext userContext) : ControllerBase
{
    [HttpPost("run")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateSubmission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> CreateRunSubmission(
        [FromBody] CreateRunSubmissionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<Guid>.Unauthorized());

        return this.ToActionResult(
            await submissionService.CreateSubmissionAsync(
                new CreateSubmissionDto(
                    request.ProblemSetupId,
                    Domain.Submissions.Enums.SubmissionType.Run,
                    request.Code,
                    userContext.User.Id,
                    request.CustomTestCases?.Select(tc => new CreateSubmissionCustomTestCaseDto(tc.Inputs)).ToArray(),
                    request.AdditionalFiles?.Select(f => new CreateSubmissionFileDto(f.Path, f.Content)).ToArray()
                ),
                cancellationToken
            )
        );
    }

    [HttpPost("grade")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.CreateSubmission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> CreateGradeSubmission(
        [FromBody] CreateGradeSubmissionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<Guid>.Unauthorized());

        return this.ToActionResult(
            await submissionService.CreateSubmissionAsync(
                new CreateSubmissionDto(
                    request.ProblemSetupId,
                    Domain.Submissions.Enums.SubmissionType.Submit,
                    request.Code,
                    userContext.User.Id,
                    null,
                    request.AdditionalFiles?.Select(f => new CreateSubmissionFileDto(f.Path, f.Content)).ToArray()
                ),
                cancellationToken
            )
        );
    }

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminSubmissions)]
    [EnableRateLimiting(WellKnownPolicies.General)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PageResult<AdminSubmissionListItemDto>>> GetAdminSubmissions(
        [FromQuery] GetAdminSubmissionsPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await submissionService.GetAdminSubmissionsPageableAsync(
                new PaginationRequest
                {
                    Page = query.Page,
                    Size = query.Size,
                    Timestamp = query.Timestamp,
                },
                query.Id,
                cancellationToken
            )
        );
    }

    [HttpGet("admin/{submissionId:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminSubmissions)]
    [EnableRateLimiting(WellKnownPolicies.General)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSubmissionDetailDto>> GetAdminSubmissionDetail(
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await submissionService.GetAdminSubmissionDetailAsync(submissionId, cancellationToken)
        );
    }

    [HttpGet("{submissionId:guid}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionStatusDto>> GetSubmissionStatus(
        Guid submissionId,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<SubmissionStatusDto>.Unauthorized());

        return this.ToActionResult(
            await submissionService.GetSubmissionStatusAsync(submissionId, userContext.User.Id, cancellationToken)
        );
    }
}