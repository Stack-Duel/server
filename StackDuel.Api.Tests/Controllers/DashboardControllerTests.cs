using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Services.Dashboard;

namespace StackDuel.Api.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetAdminDashboardStats_ReturnsTheServicesStats()
    {
        Mock<IDashboardService> service = new();
        service
            .Setup(x => x.GetAdminDashboardStatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdminDashboardStatsDto>.NotFound());

        ActionResult<AdminDashboardStatsDto> result = await ControllerAssert
            .WithHttpContext(new DashboardController(service.Object, ControllerAssert.AnonymousContext()))
            .GetAdminDashboardStats(CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task GetAdminDashboardStats_AsksForAThirtyDayNewUserWindow()
    {
        Mock<IDashboardService> service = new();
        service
            .Setup(x => x.GetAdminDashboardStatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdminDashboardStatsDto>.NotFound());

        await ControllerAssert
            .WithHttpContext(new DashboardController(service.Object, ControllerAssert.AnonymousContext()))
            .GetAdminDashboardStats(CancellationToken.None);

        service.Verify(x => x.GetAdminDashboardStatsAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyDashboardStats_ReturnsTheServicesStatsForTheCurrentUser()
    {
        Guid userId = Guid.NewGuid();
        Mock<IDashboardService> service = new();
        service
            .Setup(x => x.GetMyDashboardStatsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserDashboardStatsDto(4, 3, 7)));

        ActionResult<UserDashboardStatsDto> result = await ControllerAssert
            .WithHttpContext(new DashboardController(service.Object, ControllerAssert.AuthenticatedContext(userId)))
            .GetMyDashboardStats(CancellationToken.None);

        Assert.Equal(new UserDashboardStatsDto(4, 3, 7), ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetMyDashboardStats_IsUnauthorizedWithoutASignedInUser()
    {
        Mock<IDashboardService> service = new();

        ActionResult<UserDashboardStatsDto> result = await ControllerAssert
            .WithHttpContext(new DashboardController(service.Object, ControllerAssert.AnonymousContext()))
            .GetMyDashboardStats(CancellationToken.None);

        Assert.Equal(StatusCodes.Status401Unauthorized, ControllerAssert.StatusCodeOf(result));
        service.Verify(
            x => x.GetMyDashboardStatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}