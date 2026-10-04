using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Queries.Dashboard.GetAdminDashboardStats;
using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Services.Dashboard;

public interface IDashboardService
{
    Task<Result<AdminDashboardStatsDto>> GetAdminDashboardStatsAsync(
        int newUsersDays,
        CancellationToken cancellationToken
    );
}

internal sealed class DashboardService(IMediator mediator) : IDashboardService
{
    public async Task<Result<AdminDashboardStatsDto>> GetAdminDashboardStatsAsync(
        int newUsersDays,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetAdminDashboardStatsQuery(newUsersDays), cancellationToken);
    }
}