using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Api.Requests.Leaderboard;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Leaderboards;

namespace StackDuel.Api.Tests.Controllers;

public class LeaderboardControllerTests
{
    private static Mock<ILeaderboardService> ServiceReturningPage(PageResult<LeaderboardEntryDto>? page = null)
    {
        Mock<ILeaderboardService> service = new();
        service
            .Setup(x =>
                x.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                Result<PageResult<LeaderboardEntryDto>>.Success(page ?? ControllerAssert.Page<LeaderboardEntryDto>())
            );
        return service;
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsTheServicesPage()
    {
        PageResult<LeaderboardEntryDto> page = ControllerAssert.Page<LeaderboardEntryDto>();
        LeaderboardController controller = ControllerAssert.WithHttpContext(
            new LeaderboardController(ServiceReturningPage(page).Object, ControllerAssert.AnonymousContext())
        );

        ActionResult<PageResult<LeaderboardEntryDto>> result = await controller.GetLeaderboard(
            new GetLeaderboardRequest("duel", 300, 1, 20),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Same(page, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetLeaderboard_ForwardsTheQueryStringParameters()
    {
        Mock<ILeaderboardService> service = ServiceReturningPage();
        LeaderboardController controller = ControllerAssert.WithHttpContext(
            new LeaderboardController(service.Object, ControllerAssert.AnonymousContext())
        );

        await controller.GetLeaderboard(new GetLeaderboardRequest("solo-rush", 600, 2, 25), CancellationToken.None);

        service.Verify(
            x =>
                x.GetLeaderboardAsync(
                    "solo-rush",
                    600,
                    It.Is<PaginationRequest>(p => p.Page == 2 && p.Size == 25),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLeaderboard_Anonymously_AsksWithoutARequestingUser()
    {
        Mock<ILeaderboardService> service = ServiceReturningPage();
        LeaderboardController controller = ControllerAssert.WithHttpContext(
            new LeaderboardController(service.Object, ControllerAssert.AnonymousContext())
        );

        await controller.GetLeaderboard(new GetLeaderboardRequest("duel", 300, 1, 20), CancellationToken.None);

        service.Verify(
            x =>
                x.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<PaginationRequest>(),
                    null,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLeaderboard_SignedIn_PassesTheCallersUserIdSoTheirRowCanBeMarked()
    {
        Guid userId = Guid.NewGuid();
        Mock<ILeaderboardService> service = ServiceReturningPage();
        LeaderboardController controller = ControllerAssert.WithHttpContext(
            new LeaderboardController(service.Object, ControllerAssert.AuthenticatedContext(userId))
        );

        await controller.GetLeaderboard(new GetLeaderboardRequest("duel", 300, 1, 20), CancellationToken.None);

        service.Verify(
            x =>
                x.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<PaginationRequest>(),
                    userId,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetMyLeaderboardEntry_Anonymously_IsUnauthorizedAndNeverCallsTheService()
    {
        Mock<ILeaderboardService> service = new();
        LeaderboardController controller = ControllerAssert.WithHttpContext(
            new LeaderboardController(service.Object, ControllerAssert.AnonymousContext())
        );

        ActionResult<MyLeaderboardEntryDto> result = await controller.GetMyLeaderboardEntry(
            new GetMyLeaderboardEntryRequest("duel", 300),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status401Unauthorized, ControllerAssert.StatusCodeOf(result));
        service.Verify(
            x =>
                x.GetMyEntryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetMyLeaderboardEntry_SignedIn_AsksForThatUsersEntry()
    {
        Guid userId = Guid.NewGuid();
        Mock<ILeaderboardService> service = new();
        service
            .Setup(x =>
                x.GetMyEntryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result<MyLeaderboardEntryDto>.NotFound());
        LeaderboardController controller = ControllerAssert.WithHttpContext(
            new LeaderboardController(service.Object, ControllerAssert.AuthenticatedContext(userId))
        );

        ActionResult<MyLeaderboardEntryDto> result = await controller.GetMyLeaderboardEntry(
            new GetMyLeaderboardEntryRequest("duel", 300),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
        service.Verify(x => x.GetMyEntryAsync("duel", 300, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}