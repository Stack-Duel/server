using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Notifications;
using StackDuel.Application.Queries.Games.GetGameState;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.Games;
using System.Security.Claims;

namespace StackDuel.Api.Hubs;

/// <summary>
/// Pushes game-lifecycle events (currently: completion, progress, quick reactions) to clients
/// watching a specific game. Clients join a per-game group via <see cref="JoinGame"/> once
/// connected; the server-side broadcast (<see cref="Notifications.SignalRGameNotificationService"/>)
/// targets that group whenever something happens, regardless of which code path triggered it
/// (message consumer, sweep-job backstop, forfeit, or — for reactions — this Hub directly).
///
/// This is purely a latency optimization / ephemeral live signal — nothing about game correctness
/// depends on it, so failures here are logged, not thrown as fatal.
/// </summary>
[Authorize]
public sealed partial class GameHub(
    IMediator mediator,
    IUserService userService,
    IGameNotificationService gameNotificationService,
    ILogger<GameHub> logger
) : Hub
{
    public static string GroupNameFor(Guid gameId) => $"game:{gameId}";

    /// <summary>
    /// Subscribes the caller's connection to updates for the given game. Reuses the same
    /// GetGameStateQuery the REST endpoint uses so group membership requires the same
    /// participant/permission check — a client can't listen in on a game it couldn't otherwise
    /// fetch.
    /// </summary>
    public async Task JoinGame(Guid gameId)
    {
        Guid? userId = await ResolveUserIdAsync();
        if (userId is null)
        {
            throw new HubException("Unable to resolve the caller's account.");
        }

        Result<GameStateDto> result = await GetGameStateForCallerAsync(gameId, userId.Value);

        if (!result.IsSuccess)
        {
            LogJoinDenied(gameId, userId.Value, result.Status.ToString());
            throw new HubException("You don't have access to that game.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameFor(gameId), Context.ConnectionAborted);
    }

    public async Task LeaveGame(Guid gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameFor(gameId), Context.ConnectionAborted);
    }

    /// <summary>
    /// Sends a quick emoji reaction to whoever's watching the game. Ephemeral and player-initiated
    /// rather than a byproduct of persisted state — there's nothing to store or query back — so
    /// this is a direct Hub method with its own inline authorization check, same reasoning as
    /// <see cref="JoinGame"/>, rather than a MediatR command.
    /// </summary>
    public async Task SendReaction(Guid gameId, string emoji)
    {
        Guid? userId = await ResolveUserIdAsync();
        if (userId is null)
        {
            throw new HubException("Unable to resolve the caller's account.");
        }

        if (!GameReactionEmojis.Allowed.Contains(emoji))
        {
            throw new HubException("Unsupported reaction.");
        }

        Result<GameStateDto> result = await GetGameStateForCallerAsync(gameId, userId.Value);

        if (!result.IsSuccess)
        {
            LogReactionDenied(gameId, userId.Value, result.Status.ToString());
            throw new HubException("You don't have access to that game.");
        }

        await gameNotificationService.NotifyGameParticipantReactedAsync(
            gameId,
            userId.Value,
            emoji,
            DateTime.UtcNow,
            Context.ConnectionAborted
        );
    }

    private async Task<Result<GameStateDto>> GetGameStateForCallerAsync(Guid gameId, Guid userId) =>
        await mediator.Send(new GetGameStateQuery(gameId, userId), Context.ConnectionAborted);

    private async Task<Guid?> ResolveUserIdAsync()
    {
        string? sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
            return null;

        var result = await userService.GetBySubAsync(sub, Context.ConnectionAborted);
        return result.IsSuccess ? result.Value.Id : null;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Connection denied join to game {GameId} group for user {UserId}: {Status}."
    )]
    private partial void LogJoinDenied(Guid gameId, Guid userId, string status);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reaction denied for game {GameId} from user {UserId}: {Status}."
    )]
    private partial void LogReactionDenied(Guid gameId, Guid userId, string status);
}