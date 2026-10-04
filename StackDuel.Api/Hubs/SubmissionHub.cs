using StackDuel.Application.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace StackDuel.Api.Hubs;

/// <summary>
/// Pushes submission lifecycle events to the submitting user.
/// On connect each authenticated user is automatically added to a personal
/// <c>user:{userId}</c> group; the server-side broadcast
/// (<see cref="Notifications.SignalRSubmissionNotificationService"/>) targets that group
/// whenever any of the user's submissions reach a terminal state.
///
/// Kept separate from <see cref="GameHub"/> so submission notifications work on any page
/// (standalone problem editor, game workspace, etc.) without coupling to game logic.
/// </summary>
[Authorize]
public sealed class SubmissionHub(IUserService userService) : Hub
{
    public static string UserGroupName(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        Guid? userId = await ResolveUserIdAsync();
        if (userId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId.Value), Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    private async Task<Guid?> ResolveUserIdAsync()
    {
        string? sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
            return null;

        var result = await userService.GetBySubAsync(sub, Context.ConnectionAborted);
        return result.IsSuccess ? result.Value.Id : null;
    }
}