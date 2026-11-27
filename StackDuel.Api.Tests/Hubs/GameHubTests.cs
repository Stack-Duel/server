using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Api.Hubs;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Notifications;
using StackDuel.Application.Queries.Games.GetGameAccess;
using StackDuel.Application.Queries.Games.GetGameState;
using StackDuel.Application.Services.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Api.Tests.Hubs;

public class GameHubTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IGameNotificationService> _notifications = new();
    private readonly Guid _gameId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public GameHubTests()
    {
        _userService
            .Setup(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result<UserDto>.Success(
                    new UserDto(_userId, "auth0|abc", "alice", null, null, false, null, default, null, [])
                )
            );
    }

    private GameHub CreateHub(out HubTestContext context)
    {
        GameHub hub = new(
            _mediator.Object,
            _userService.Object,
            _notifications.Object,
            NullLogger<GameHub>.Instance
        );
        context = new HubTestContext();
        context.AttachTo(hub);
        return hub;
    }

    private void SetupGameState(Result<GameStateDto> result) =>
        _mediator
            .Setup(x => x.Send(It.IsAny<GetGameStateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private void SetupGameAccess(Result<GameAccessDto> result) =>
        _mediator
            .Setup(x => x.Send(It.IsAny<GetGameAccessQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public void GroupNameFor_IsNamespacedByGameId()
    {
        Assert.Equal($"game:{_gameId}", GameHub.GroupNameFor(_gameId));
    }

    [Fact]
    public async Task JoinGame_AddsTheConnectionToTheGameGroupWhenTheCallerIsAuthorized()
    {
        SetupGameState(Result<GameStateDto>.Success(null!));
        GameHub hub = CreateHub(out HubTestContext context);

        await hub.JoinGame(_gameId);

        context.Groups.Verify(
            x => x.AddToGroupAsync(HubTestContext.ConnectionId, $"game:{_gameId}", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task JoinGame_ThrowsWhenTheGameStateQueryDeniesTheCaller()
    {
        SetupGameState(Result<GameStateDto>.Forbidden());
        GameHub hub = CreateHub(out HubTestContext context);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinGame(_gameId));

        context.Groups.Verify(
            x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task JoinGame_RejectsAnEmptyGameId()
    {
        GameHub hub = CreateHub(out _);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinGame(Guid.Empty));

        _mediator.Verify(x => x.Send(It.IsAny<GetGameStateQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LeaveGame_RemovesTheConnectionFromTheGameGroup()
    {
        GameHub hub = CreateHub(out HubTestContext context);

        await hub.LeaveGame(_gameId);

        context.Groups.Verify(
            x =>
                x.RemoveFromGroupAsync(
                    HubTestContext.ConnectionId,
                    $"game:{_gameId}",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task LeaveGame_RejectsAnEmptyGameId()
    {
        GameHub hub = CreateHub(out HubTestContext context);

        await Assert.ThrowsAsync<HubException>(() => hub.LeaveGame(Guid.Empty));

        context.Groups.Verify(
            x => x.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendReaction_BroadcastsAnAllowedEmojiForARunningGame()
    {
        SetupGameAccess(Result<GameAccessDto>.Success(new GameAccessDto(GameStatus.Running)));
        GameHub hub = CreateHub(out _);

        await hub.SendReaction(_gameId, "🔥");

        _notifications.Verify(
            x => x.NotifyGameParticipantReactedAsync(_gameId, _userId, "🔥", It.IsAny<DateTime>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SendReaction_RejectsAnEmojiOutsideTheAllowList()
    {
        GameHub hub = CreateHub(out _);

        await Assert.ThrowsAsync<HubException>(() => hub.SendReaction(_gameId, "🙃"));

        _mediator.Verify(x => x.Send(It.IsAny<GetGameAccessQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoReaction();
    }

    [Fact]
    public async Task SendReaction_RejectsAnEmptyGameId()
    {
        GameHub hub = CreateHub(out _);

        await Assert.ThrowsAsync<HubException>(() => hub.SendReaction(Guid.Empty, "🔥"));

        VerifyNoReaction();
    }

    [Fact]
    public async Task SendReaction_ThrowsWhenTheCallerIsNotAParticipant()
    {
        SetupGameAccess(Result<GameAccessDto>.Forbidden());
        GameHub hub = CreateHub(out _);

        await Assert.ThrowsAsync<HubException>(() => hub.SendReaction(_gameId, "🔥"));

        VerifyNoReaction();
    }

    [Theory]
    [InlineData(GameStatus.Pending)]
    [InlineData(GameStatus.Completed)]
    public async Task SendReaction_ThrowsWhenTheGameIsNotRunning(GameStatus status)
    {
        SetupGameAccess(Result<GameAccessDto>.Success(new GameAccessDto(status)));
        GameHub hub = CreateHub(out _);

        await Assert.ThrowsAsync<HubException>(() => hub.SendReaction(_gameId, "🔥"));

        VerifyNoReaction();
    }

    [Fact]
    public async Task SendReaction_ThrowsOnASecondReactionInsideTheCooldown()
    {
        SetupGameAccess(Result<GameAccessDto>.Success(new GameAccessDto(GameStatus.Running)));
        GameHub hub = CreateHub(out _);

        await hub.SendReaction(_gameId, "🔥");

        await Assert.ThrowsAsync<HubException>(() => hub.SendReaction(_gameId, "👍"));

        _notifications.Verify(
            x =>
                x.NotifyGameParticipantReactedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendReaction_ResolvesTheCallersAccountOnlyOncePerConnection()
    {
        SetupGameAccess(Result<GameAccessDto>.Success(new GameAccessDto(GameStatus.Running)));
        SetupGameState(Result<GameStateDto>.Success(null!));
        GameHub hub = CreateHub(out _);

        await hub.JoinGame(_gameId);
        await hub.SendReaction(_gameId, "🔥");

        _userService.Verify(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinGame_ThrowsWhenTheCallersAccountCannotBeResolved()
    {
        _userService
            .Setup(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.NotFound());
        GameHub hub = CreateHub(out _);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinGame(_gameId));
    }

    [Fact]
    public async Task JoinGame_ThrowsWhenThereIsNoSubjectClaim()
    {
        GameHub hub = new(
            _mediator.Object,
            _userService.Object,
            _notifications.Object,
            NullLogger<GameHub>.Instance
        );
        HubTestContext context = new(sub: null);
        context.AttachTo(hub);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinGame(_gameId));
    }

    private void VerifyNoReaction() =>
        _notifications.Verify(
            x =>
                x.NotifyGameParticipantReactedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()
                ),
            Times.Never
        );
}
