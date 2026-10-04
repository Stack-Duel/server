using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Users.Dtos;

public sealed record GameModeStatDto(
    string GameModeKey,
    string GameModeName,
    bool HasOpponents,
    int GamesPlayed,
    int Wins,
    int Losses,
    int Draws,
    int BestScore
);

public sealed record ProfileGameParticipantDto(string Username, string? ImageUrl, int Score);

public sealed record ProfileGameDto(
    Guid GameId,
    string GameModeKey,
    string GameModeName,
    DateTime? EndedAt,
    IReadOnlyList<ProfileGameParticipantDto> Participants
);

public sealed record ProfileSubmissionDto(
    Guid Id,
    SubmissionStatus Status,
    string ProblemTitle,
    string ProblemSlug,
    SubmissionLanguageDto Language,
    DateTime CreatedAt
);

public sealed record SubmissionDayCountDto(DateOnly Date, int Count);

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string? Bio,
    string? ImageUrl,
    DateTime CreatedAt,
    bool IsPrivate,
    bool IsOwnProfile,
    IReadOnlyList<GameModeStatDto>? GameModeStats,
    IReadOnlyList<ProfileGameDto>? RecentGames,
    IReadOnlyList<ProfileSubmissionDto>? RecentSubmissions,
    IReadOnlyList<SubmissionDayCountDto>? SubmissionCalendar = null,
    DateOnly? SubmissionCalendarRangeStart = null,
    DateOnly? SubmissionCalendarRangeEnd = null
);

public sealed record UserGameStatsDto(
    Guid Id,
    string Username,
    bool IsOwnProfile,
    IReadOnlyList<GameModeStatDto>? GameModeStats
);