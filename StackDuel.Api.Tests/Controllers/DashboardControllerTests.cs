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
            .WithHttpContext(new DashboardController(service.Object))
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
            .WithHttpContext(new DashboardController(service.Object))
            .GetAdminDashboardStats(CancellationToken.None);

        service.Verify(x => x.GetAdminDashboardStatsAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }
}