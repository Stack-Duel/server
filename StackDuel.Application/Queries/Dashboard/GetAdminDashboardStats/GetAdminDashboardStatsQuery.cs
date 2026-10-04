using StackDuel.Application.Dashboard.Dtos;

namespace StackDuel.Application.Queries.Dashboard.GetAdminDashboardStats;

public sealed record GetAdminDashboardStatsQuery(int NewUsersDays) : IQuery<AdminDashboardStatsDto>;