using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Dashboard;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Domain.Feedback.Enums;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Dashboard;

internal sealed class DashboardReadRepository(StackDuelDbContext context) : IDashboardReadRepository
{
    public async Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync(
        int newUsersDays,
        CancellationToken cancellationToken = default
    )
    {
        int totalUsers = await context.Users.AsNoTracking().CountAsync(cancellationToken);
        int totalProblems = await context.Problems.AsNoTracking().CountAsync(cancellationToken);
        int totalGames = await context.Games.AsNoTracking().CountAsync(cancellationToken);
        int totalSubmissions = await context.Submissions.AsNoTracking().CountAsync(cancellationToken);

        var newUsersByDay = await GetNewUsersByDayAsync(newUsersDays, cancellationToken);
        var feedbackByStatus = await GetFeedbackByStatusAsync(cancellationToken);

        return new AdminDashboardStatsDto(
            totalUsers,
            totalProblems,
            totalGames,
            totalSubmissions,
            newUsersByDay,
            feedbackByStatus
        );
    }

    private async Task<IReadOnlyList<DailyUserCountDto>> GetNewUsersByDayAsync(
        int newUsersDays,
        CancellationToken cancellationToken
    )
    {
        DateTime cutoff = DateTime.UtcNow.Date.AddDays(-(newUsersDays - 1));

        var rawCounts = await context
            .Users.AsNoTracking()
            .Where(u => u.CreatedAt >= cutoff)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countsByDate = rawCounts.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        DateOnly startDate = DateOnly.FromDateTime(cutoff);

        return Enumerable
            .Range(0, newUsersDays)
            .Select(offset => startDate.AddDays(offset))
            .Select(date => new DailyUserCountDto(date, countsByDate.GetValueOrDefault(date)))
            .ToList();
    }

    private async Task<IReadOnlyList<FeedbackStatusCountDto>> GetFeedbackByStatusAsync(
        CancellationToken cancellationToken
    )
    {
        var rawCounts = await context
            .FeedbackSubmissions.AsNoTracking()
            .GroupBy(f => f.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countsByStatus = rawCounts.ToDictionary(x => x.Status, x => x.Count);

        return Enum.GetValues<FeedbackStatus>()
            .Select(status => new FeedbackStatusCountDto(status, countsByStatus.GetValueOrDefault(status)))
            .ToList();
    }
}