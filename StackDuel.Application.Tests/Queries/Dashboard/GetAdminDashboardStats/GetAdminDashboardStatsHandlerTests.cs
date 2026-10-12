using Moq;
using StackDuel.Application.Dashboard;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Domain.Feedback.Enums;
using GetAdminDashboardStatsHandler = StackDuel.Application.Queries.Dashboard.GetAdminDashboardStats.GetAdminDashboardStatsHandler;
using GetAdminDashboardStatsQuery = StackDuel.Application.Queries.Dashboard.GetAdminDashboardStats.GetAdminDashboardStatsQuery;

namespace StackDuel.Application.Tests.Queries.Dashboard.GetAdminDashboardStats;

public class GetAdminDashboardStatsHandlerTests
{
    private Mock<IDashboardReadRepository> _dashboardReadRepository = null!;
    private GetAdminDashboardStatsHandler _handler = null!;

    public GetAdminDashboardStatsHandlerTests()
    {
        _dashboardReadRepository = new Mock<IDashboardReadRepository>();
        _handler = new GetAdminDashboardStatsHandler(_dashboardReadRepository.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToRepositoryWithRequestedWindow_ReturnsStats()
    {
        var stats = new AdminDashboardStatsDto(
            TotalUsers: 10,
            TotalProblems: 5,
            TotalGames: 3,
            TotalSubmissions: 42,
            NewUsersByDay: [new DailyUserCountDto(new DateOnly(2026, 8, 30), 2)],
            FeedbackByStatus: [new FeedbackStatusCountDto(FeedbackStatus.New, 1)]
        );

        _dashboardReadRepository
            .Setup(r => r.GetAdminDashboardStatsAsync(30, CancellationToken.None))
            .ReturnsAsync(stats);

        var result = await _handler.Handle(new GetAdminDashboardStatsQuery(30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(stats, result.Value);
        _dashboardReadRepository.Verify(r => r.GetAdminDashboardStatsAsync(30, CancellationToken.None), Times.Once);
    }
}