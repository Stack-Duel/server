using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Api.Requests.Audit;
using StackDuel.Api.Requests.Leaderboard;
using StackDuel.Application;
using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Groups.Dtos;
using StackDuel.Application.Languages.Dtos;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Audit;
using StackDuel.Application.Services.Dashboard;
using StackDuel.Application.Services.Groups;
using StackDuel.Application.Services.Languages;
using StackDuel.Application.Services.Leaderboards;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Api.Tests.Controllers;

internal static class ControllerAssert
{
    internal static int StatusCodeOf<T>(ActionResult<T> result) =>
        result.Result switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            null => StatusCodes.Status200OK,
            _ => -1,
        };

    internal static TValue ValueOf<TValue>(ActionResult<TValue> result) =>
        result.Value ?? (TValue)((ObjectResult)result.Result!).Value!;

    internal static UserContext AuthenticatedContext(Guid? userId = null) =>
        new()
        {
            User = new UserDto(
                userId ?? Guid.NewGuid(),
                "auth0|sub",
                "player",
                null,
                null,
                false,
                null,
                DateTime.UtcNow,
                null,
                []
            ),
        };

    internal static UserContext AnonymousContext() => new();

    internal static TController WithHttpContext<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            ActionDescriptor = new ControllerActionDescriptor(),
        };
        return controller;
    }

    internal static PageResult<T> Page<T>(params T[] items) =>
        new()
        {
            Results = items,
            Total = items.Length,
            Page = 1,
            Size = 20,
        };
}

public class LanguageControllerTests
{
    [Fact]
    public async Task GetLanguages_ReturnsTheServicesLanguages()
    {
        List<LanguageDto> languages = [new(Guid.NewGuid(), "Python", [new LanguageVersionDto(Guid.NewGuid(), "3.12")])];
        Mock<ILanguageService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.Success(languages));

        ActionResult<IReadOnlyList<LanguageDto>> result = await ControllerAssert
            .WithHttpContext(new LanguageController(service.Object))
            .GetLanguages(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(languages, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetLanguages_PassesTheCancellationTokenThrough()
    {
        using CancellationTokenSource cts = new();
        Mock<ILanguageService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.Success([]));

        await ControllerAssert.WithHttpContext(new LanguageController(service.Object)).GetLanguages(cts.Token);

        service.Verify(x => x.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetLanguages_ServiceFailure_IsSurfacedAsAnErrorStatus()
    {
        Mock<ILanguageService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.NotFound());

        ActionResult<IReadOnlyList<LanguageDto>> result = await ControllerAssert
            .WithHttpContext(new LanguageController(service.Object))
            .GetLanguages(CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }
}

public class GroupControllerTests
{
    [Fact]
    public async Task GetGroups_ReturnsTheServicesGroups()
    {
        List<GroupDto> groups = [new(Guid.NewGuid(), "admin")];
        Mock<IGroupService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GroupDto>>.Success(groups));

        ActionResult<IReadOnlyList<GroupDto>> result = await ControllerAssert
            .WithHttpContext(new GroupController(service.Object))
            .GetGroups(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(groups, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetGroups_PassesTheCancellationTokenThrough()
    {
        using CancellationTokenSource cts = new();
        Mock<IGroupService> service = new();
        service
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GroupDto>>.Success([]));

        await ControllerAssert.WithHttpContext(new GroupController(service.Object)).GetGroups(cts.Token);

        service.Verify(x => x.GetAllAsync(cts.Token), Times.Once);
    }
}

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

public class AuditLogControllerTests
{
    [Fact]
    public async Task GetAuditLog_ReturnsTheServicesPage()
    {
        PageResult<AuditLogEntryDto> page = ControllerAssert.Page<AuditLogEntryDto>();
        Mock<IAuditLogService> service = new();
        service
            .Setup(x => x.GetPageableAsync(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PageResult<AuditLogEntryDto>>.Success(page));

        ActionResult<PageResult<AuditLogEntryDto>> result = await ControllerAssert
            .WithHttpContext(new AuditLogController(service.Object))
            .GetAuditLog(new GetAuditLogPageableRequest(1, 20, DateTime.UnixEpoch), CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Same(page, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetAuditLog_ForwardsPageSizeAndTimestampFromTheQueryString()
    {
        DateTime timestamp = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        PaginationRequest? captured = null;
        Mock<IAuditLogService> service = new();
        service
            .Setup(x => x.GetPageableAsync(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PaginationRequest, CancellationToken>((pagination, _) => captured = pagination)
            .ReturnsAsync(Result<PageResult<AuditLogEntryDto>>.Success(ControllerAssert.Page<AuditLogEntryDto>()));

        await ControllerAssert
            .WithHttpContext(new AuditLogController(service.Object))
            .GetAuditLog(new GetAuditLogPageableRequest(3, 50, timestamp), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(3, captured!.Page);
        Assert.Equal(50, captured.Size);
        Assert.Equal(timestamp, captured.Timestamp);
    }
}

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