using Ardalis.Result;
using StackDuel.Application.Dashboard;
using StackDuel.Application.Dashboard.Dtos;

namespace StackDuel.Application.Queries.Dashboard.GetAdminDashboardStats;

internal sealed class GetAdminDashboardStatsHandler(IDashboardReadRepository dashboardReadRepository)
    : IQueryHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    public async Task<Result<AdminDashboardStatsDto>> Handle(
        GetAdminDashboardStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        var stats = await dashboardReadRepository.GetAdminDashboardStatsAsync(request.NewUsersDays, cancellationToken);

        return Result.Success(stats);
    }
}