using StackDuel.Application.Games;
using StackDuel.Application.Submissions;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Users.GetUserProfileByUsername;

internal sealed class GetUserProfileByUsernameHandler(
    IUserReadRepository userReadRepository,
    IGameReadRepository gameReadRepository,
    ISubmissionReadRepository submissionReadRepository
) : IQueryHandler<GetUserProfileByUsernameQuery, UserProfileDto>
{
    private const int RecentGamesLimit = 10;
    private const int RecentSubmissionsLimit = 10;
    private const int SubmissionCalendarMonths = 12;

    public async Task<Result<UserProfileDto>> Handle(
        GetUserProfileByUsernameQuery request,
        CancellationToken cancellationToken
    )
    {
        var profile = await userReadRepository.FindProfileByUsernameAsync(request.Username, cancellationToken);

        if (profile is null)
            return Result.NotFound();

        bool isOwnProfile = request.RequestingUserId is not null && request.RequestingUserId == profile.Id;
        bool showPrivateSections = isOwnProfile || !profile.IsPrivate;

        if (!showPrivateSections)
            return Result.Success(profile with { IsOwnProfile = isOwnProfile });

        var (gameModeStats, recentGames) = await BuildGameSectionsAsync(profile.Id, cancellationToken);
        var recentSubmissions = await submissionReadRepository.GetRecentSubmissionsForUserAsync(
            profile.Id,
            RecentSubmissionsLimit,
            cancellationToken
        );

        DateOnly rangeEnd = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly rangeStart = rangeEnd.AddMonths(-SubmissionCalendarMonths);
        var submissionCalendar = await BuildSubmissionCalendarAsync(profile.Id, rangeStart, cancellationToken);

        return Result.Success(
            profile with
            {
                IsOwnProfile = isOwnProfile,
                GameModeStats = gameModeStats,
                RecentGames = recentGames,
                RecentSubmissions = recentSubmissions,
                SubmissionCalendar = submissionCalendar,
                SubmissionCalendarRangeStart = rangeStart,
                SubmissionCalendarRangeEnd = rangeEnd,
            }
        );
    }

    // Days with zero submissions are implied by their absence, not sent over the wire — the
    // caller already knows the full window via SubmissionCalendarRangeStart/End and fills gaps
    // itself when it needs a contiguous grid.
    private async Task<IReadOnlyList<SubmissionDayCountDto>> BuildSubmissionCalendarAsync(
        Guid userId,
        DateOnly rangeStart,
        CancellationToken cancellationToken
    )
    {
        var counts = await submissionReadRepository.GetSubmissionCountsByDayForUserAsync(
            userId,
            rangeStart,
            cancellationToken
        );

        return [.. counts.Where(c => c.Count > 0).OrderBy(c => c.Date)];
    }

    private async Task<(
        IReadOnlyList<GameModeStatDto> Stats,
        IReadOnlyList<ProfileGameDto> RecentGames
    )> BuildGameSectionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        GameModeStatsResult statsResult = await GameModeStatsBuilder.BuildAsync(
            gameReadRepository,
            userId,
            cancellationToken
        );

        List<Game> recentGameEntities = [.. statsResult.GamesWithKnownMode.Take(RecentGamesLimit)];
        Guid[] participantUserIds =
        [
            .. recentGameEntities.SelectMany(g => g.Participants).Select(p => p.UserId).Distinct(),
        ];
        var usersById = await userReadRepository.FindByIdsAsync(participantUserIds, cancellationToken);

        List<ProfileGameDto> recentGames =
        [
            .. recentGameEntities.Select(g =>
            {
                GameMode mode = statsResult.GameModesById[g.GameModeId];

                List<ProfileGameParticipantDto> participants =
                [
                    .. g
                        .Participants.OrderByDescending(p => p.Score)
                        .Select(p =>
                        {
                            usersById.TryGetValue(p.UserId, out var user);
                            return new ProfileGameParticipantDto(user?.Username ?? "Unknown", user?.ImageUrl, p.Score);
                        }),
                ];

                return new ProfileGameDto(g.Id, mode.Key, mode.Name, g.EndedAt, participants);
            }),
        ];

        return (statsResult.Stats, recentGames);
    }
}