using Ardalis.Result;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Api.Hubs;
using StackDuel.Application.Services.Users;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Api.Tests.Hubs;

public class SubmissionHubTests
{
    private static UserDto User(Guid id) => new(id, "auth0|abc", "alice", null, null, false, null, default, null, []);

    private static Mock<IUserService> UserServiceReturning(Result<UserDto> result)
    {
        Mock<IUserService> userService = new();
        userService.Setup(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return userService;
    }

    [Fact]
    public void UserGroupName_IsNamespacedByUserId()
    {
        Guid userId = Guid.NewGuid();

        Assert.Equal($"user:{userId}", SubmissionHub.UserGroupName(userId));
    }

    [Fact]
    public async Task OnConnectedAsync_AddsTheResolvedUserToTheirPersonalGroup()
    {
        Guid userId = Guid.NewGuid();
        SubmissionHub hub = new(UserServiceReturning(Result<UserDto>.Success(User(userId))).Object, NullLogger<SubmissionHub>.Instance);
        HubTestContext context = new();
        context.AttachTo(hub);

        await hub.OnConnectedAsync();

        context.Groups.Verify(
            x =>
                x.AddToGroupAsync(
                    HubTestContext.ConnectionId,
                    $"user:{userId}",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task OnConnectedAsync_RejectsTheConnectionWhenTheAccountCannotBeResolved()
    {
        SubmissionHub hub = new(UserServiceReturning(Result<UserDto>.NotFound()).Object, NullLogger<SubmissionHub>.Instance);
        HubTestContext context = new();
        context.AttachTo(hub);

        await Assert.ThrowsAsync<HubException>(hub.OnConnectedAsync);

        context.Groups.Verify(
            x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task OnConnectedAsync_RejectsTheConnectionWhenThereIsNoSubjectClaim()
    {
        Mock<IUserService> userService = new();
        SubmissionHub hub = new(userService.Object, NullLogger<SubmissionHub>.Instance);
        HubTestContext context = new(sub: null);
        context.AttachTo(hub);

        await Assert.ThrowsAsync<HubException>(hub.OnConnectedAsync);

        userService.Verify(x => x.GetBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
