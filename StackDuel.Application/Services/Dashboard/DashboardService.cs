using Ardalis.Result;
using MediatR;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Queries.Dashboard.GetAdminDashboardStats;
using StackDuel.Application.Queries.Dashboard.GetMyDashboardStats;

namespace StackDuel.Application.Services.Dashboard;

public interface IDashboardService
{
    Task<Result<AdminDashboardStatsDto>> GetAdminDashboardStatsAsync(
        int newUsersDays,
        CancellationToken cancellationToken
    );

    Task<Result<UserDashboardStatsDto>> GetMyDashboardStatsAsync(Guid userId, CancellationToken cancellationToken);
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

    public async Task<Result<UserDashboardStatsDto>> GetMyDashboardStatsAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetMyDashboardStatsQuery(userId), cancellationToken);
    }
}