using StackDuel.Application.Dashboard.Dtos;

namespace StackDuel.Application.Dashboard;

public interface IDashboardReadRepository
{
    Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync(
        int newUsersDays,
        CancellationToken cancellationToken = default
    );
}