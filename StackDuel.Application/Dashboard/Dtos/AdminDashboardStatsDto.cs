using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Dashboard.Dtos;

public sealed record DailyUserCountDto(DateOnly Date, int Count);

public sealed record FeedbackStatusCountDto(FeedbackStatus Status, int Count);

public sealed record AdminDashboardStatsDto(
    int TotalUsers,
    int TotalProblems,
    int TotalGames,
    int TotalSubmissions,
    IReadOnlyList<DailyUserCountDto> NewUsersByDay,
    IReadOnlyList<FeedbackStatusCountDto> FeedbackByStatus
);