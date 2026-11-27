using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Notifications;
using StackDuel.Application.Queries.Games.GetGameAccess;
using StackDuel.Application.Queries.Games.GetGameState;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Enums;
using System.Security.Claims;

namespace StackDuel.Api.Hubs;

[Authorize]
public sealed partial class GameHub(
    IMediator mediator,
    IUserService userService,
    IGameNotificationService gameNotificationService,
    ILogger<GameHub> logger
) : Hub
{
    private const string UserIdItemKey = "stackduel:userId";

    private const string LastReactionItemKey = "stackduel:lastReactionAt";

    private static readonly TimeSpan ReactionCooldown = TimeSpan.FromMilliseconds(750);

    public static string GroupNameFor(Guid gameId) => $"game:{gameId}";

    public async Task JoinGame(Guid gameId)
    {
        if (gameId == Guid.Empty)
        {
            throw new HubException("A game id is required.");
        }

        Guid userId = await RequireUserIdAsync();

        Result<GameStateDto> result = await mediator.Send(
            new GetGameStateQuery(gameId, userId),
            Context.ConnectionAborted
        );

        if (!result.IsSuccess)
        {
            LogJoinDenied(gameId, userId, result.Status);
            throw new HubException("You don't have access to that game.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameFor(gameId), Context.ConnectionAborted);
    }

    public async Task LeaveGame(Guid gameId)
    {
        if (gameId == Guid.Empty)
        {
            throw new HubException("A game id is required.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameFor(gameId), Context.ConnectionAborted);
    }

    public async Task SendReaction(Guid gameId, string emoji)
    {
        if (gameId == Guid.Empty)
        {
            throw new HubException("A game id is required.");
        }

        if (!GameReactionEmojis.Allowed.Contains(emoji))
        {
            throw new HubException("Unsupported reaction.");
        }

        Guid userId = await RequireUserIdAsync();

        if (IsCoolingDown())
        {
            throw new HubException("You're sending reactions too quickly.");
        }

        Result<GameAccessDto> result = await mediator.Send(
            new GetGameAccessQuery(gameId, userId),
            Context.ConnectionAborted
        );

        if (!result.IsSuccess)
        {
            LogReactionDenied(gameId, userId, result.Status);
            throw new HubException("You don't have access to that game.");
        }

        if (result.Value.Status != GameStatus.Running)
        {
            throw new HubException("Reactions are only available while the game is running.");
        }

        Context.Items[LastReactionItemKey] = DateTime.UtcNow;

        await gameNotificationService.NotifyGameParticipantReactedAsync(gameId, userId, emoji, DateTime.UtcNow);
    }

    private bool IsCoolingDown()
    {
        if (Context.Items.TryGetValue(LastReactionItemKey, out object? lastSentAt) && lastSentAt is DateTime sentAt)
        {
            return DateTime.UtcNow - sentAt < ReactionCooldown;
        }

        return false;
    }

    private async Task<Guid> RequireUserIdAsync()
    {
        if (Context.Items.TryGetValue(UserIdItemKey, out object? cached) && cached is Guid cachedUserId)
        {
            return cachedUserId;
        }

        string? sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            throw new HubException("Unable to resolve the caller's account.");
        }

        var result = await userService.GetBySubAsync(sub, Context.ConnectionAborted);
        if (!result.IsSuccess)
        {
            throw new HubException("Unable to resolve the caller's account.");
        }

        Context.Items[UserIdItemKey] = result.Value.Id;
        return result.Value.Id;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Connection denied join to game {GameId} group for user {UserId}: {Status}."
    )]
    private partial void LogJoinDenied(Guid gameId, Guid userId, ResultStatus status);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reaction denied for game {GameId} from user {UserId}: {Status}."
    )]
    private partial void LogReactionDenied(Guid gameId, Guid userId, ResultStatus status);
}