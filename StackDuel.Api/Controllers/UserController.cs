using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Attributes;
using StackDuel.Api.Authorization;
using StackDuel.Api.RateLimiting;
using StackDuel.Api.Requests.User;
using StackDuel.Api.Responses.User;
using StackDuel.Application;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Application.Users.Dtos.Admin;
using System.Security.Claims;

namespace StackDuel.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(WellKnownPolicies.General)]
public sealed class UserController(IUserService userService, UserContext userContext) : ControllerBase
{
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserResponse> GetAccount()
    {
        if (userContext.User is null)
            return NotFound();

        return Ok(UserResponse.FromDto(userContext.User, userContext.Permissions, userContext.Roles));
    }

    [HttpPut]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpsertAccount(
        [FromBody] UpsertUserRequest request,
        CancellationToken cancellationToken
    )
    {
        string? sub = GetSub();

        if (string.IsNullOrEmpty(sub))
        {
            return this.ToActionResult(Result.Invalid(new ValidationError("sub", "User sub is missing")));
        }

        return this.ToActionResult(
            await userService.UpsertAccountAsync(
                sub,
                new UpsertUserDto(
                    request.Username,
                    request.Picture,
                    request.Bio,
                    request.LanguageIds,
                    request.TenantId
                ),
                cancellationToken
            )
        );
    }

    [HttpGet("profile/{username}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetProfile(string username, CancellationToken cancellationToken)
    {
        return this.ToActionResult(
            await userService.GetProfileByUsernameAsync(username, userContext.User?.Id, cancellationToken)
        );
    }

    [HttpGet("profile/{username}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserGameStatsDto>> GetGameStats(
        string username,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await userService.GetGameStatsByUsernameAsync(username, userContext.User?.Id, cancellationToken)
        );
    }

    [HttpPut("profile-privacy")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateProfilePrivacy(
        [FromBody] UpdateProfilePrivacyRequest request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await userService.UpdateProfilePrivacyAsync(userContext.User.Id, request.IsPrivate, cancellationToken)
        );
    }

    [HttpPost("avatar")]
    [RequireUser]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_000_000)]
    [EnableRateLimiting(WellKnownPolicies.AvatarUpload)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<string>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<string>.Unauthorized());

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return this.ToActionResult(
            await userService.UploadAvatarAsync(userContext.User.Id, stream.ToArray(), cancellationToken)
        );
    }

    [HttpGet("avatar-history")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserAvatarDto>>> GetAvatarHistory(CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result<IReadOnlyList<UserAvatarDto>>.Unauthorized());

        return this.ToActionResult(await userService.GetAvatarHistoryAsync(userContext.User.Id, cancellationToken));
    }

    [HttpPut("avatar/{avatarId:guid}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SelectAvatar(Guid avatarId, CancellationToken cancellationToken)
    {
        if (userContext.User is null)
            return this.ToActionResult(Result.Unauthorized());

        return this.ToActionResult(
            await userService.SelectAvatarAsync(userContext.User.Id, avatarId, cancellationToken)
        );
    }

    [HttpGet("admin")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminUsers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PageResult<AdminUserDto>>> GetAdminUsers(
        [FromQuery] GetAdminUsersPageableRequest query,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(
            await userService.GetAdminUsersPageableAsync(
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

    [HttpGet("admin/{id:guid}")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.ReadAdminUsers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailDto>> GetAdminUserDetail(Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.GetAdminUserDetailAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/groups")]
    [RequireUser]
    [RequirePermission(WellKnownPermissions.UpdateAdminUserGroups)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateUserGroups(
        Guid id,
        [FromBody] UpdateUserGroupsRequest request,
        CancellationToken cancellationToken
    )
    {
        return this.ToActionResult(await userService.UpdateUserGroupsAsync(id, request.GroupIds, cancellationToken));
    }

    private string? GetSub() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}