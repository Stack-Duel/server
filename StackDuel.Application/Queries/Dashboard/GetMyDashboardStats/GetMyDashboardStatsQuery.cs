using StackDuel.Application.Dashboard.Dtos;

namespace StackDuel.Application.Queries.Dashboard.GetMyDashboardStats;

public sealed record GetMyDashboardStatsQuery(Guid UserId) : IQuery<UserDashboardStatsDto>;