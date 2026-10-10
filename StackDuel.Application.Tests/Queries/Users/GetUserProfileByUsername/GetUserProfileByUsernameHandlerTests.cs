using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Submissions;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using GetUserProfileByUsernameHandler = StackDuel.Application.Queries.Users.GetUserProfileByUsername.GetUserProfileByUsernameHandler;
using GetUserProfileByUsernameQuery = StackDuel.Application.Queries.Users.GetUserProfileByUsername.GetUserProfileByUsernameQuery;

namespace StackDuel.Application.Tests.Queries.Users.GetUserProfileByUsername;

public class GetUserProfileByUsernameHandlerTests
{
    private Mock<IUserReadRepository> _userReadRepository = null!;
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private GetUserProfileByUsernameHandler _handler = null!;

    public GetUserProfileByUsernameHandlerTests()
    {
        _userReadRepository = new Mock<IUserReadRepository>();
        _gameReadRepository = new Mock<IGameReadRepository>();
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();

        _handler = new GetUserProfileByUsernameHandler(
            _userReadRepository.Object,
            _gameReadRepository.Object,
            _submissionReadRepository.Object
        );

        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[]);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<Guid, UserDto>)new Dictionary<Guid, UserDto>());
        _submissionReadRepository
            .Setup(x =>
                x.GetRecentSubmissionsForUserAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((IReadOnlyList<ProfileSubmissionDto>)[]);
        _submissionReadRepository
            .Setup(x =>
                x.GetSubmissionCountsByDayForUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyList<SubmissionDayCountDto>)[]);
    }

    private static UserProfileDto CreateBaseProfile(Guid userId, bool isPrivate) =>
        new(userId, "alice", "bio", null, DateTime.UtcNow, isPrivate, false, null, null, null, null);

    private static Game CreateCompletedGame(Guid gameModeId, params (Guid UserId, int Score)[] participants)
    {
        var game = new Game(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], participants.Select(p => p.UserId), 600);
        game.Start();
        foreach (var (userId, score) in participants)
            for (int i = 0; i < score; i++)
                game.RecordProblemSolved(userId);
        game.Complete();
        return game;
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsNotFound()
    {
        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("missing", null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_PrivateProfile_NotOwner_ReturnsMinimalProfileWithoutFetchingGamesOrSubmissions()
    {
        var profile = CreateBaseProfile(Guid.NewGuid(), isPrivate: true);
        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsOwnProfile);
        Assert.Null(result.Value.GameModeStats);
        Assert.Null(result.Value.RecentGames);
        Assert.Null(result.Value.RecentSubmissions);
        Assert.Null(result.Value.SubmissionCalendar);
        _gameReadRepository.Verify(
            x => x.GetCompletedGamesForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _submissionReadRepository.Verify(
            x => x.GetRecentSubmissionsForUserAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _submissionReadRepository.Verify(
            x =>
                x.GetSubmissionCountsByDayForUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_PrivateProfile_Owner_FetchesPrivateSections()
    {
        var profile = CreateBaseProfile(Guid.NewGuid(), isPrivate: true);
        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", profile.Id),
            CancellationToken.None
        );

        Assert.True(result.Value.IsOwnProfile);
        Assert.NotNull(result.Value.GameModeStats);
        Assert.NotNull(result.Value.RecentGames);
        Assert.NotNull(result.Value.RecentSubmissions);
        Assert.NotNull(result.Value.SubmissionCalendar);
    }

    [Fact]
    public async Task Handle_SubmissionCalendar_OnlyIncludesDaysWithSubmissionsAndExposesRange()
    {
        var userId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _submissionReadRepository
            .Setup(x =>
                x.GetSubmissionCountsByDayForUserAsync(userId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((IReadOnlyList<SubmissionDayCountDto>)[new SubmissionDayCountDto(today, 3)]);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", userId),
            CancellationToken.None
        );

        var calendar = result.Value.SubmissionCalendar!;
        Assert.Single(calendar);
        Assert.Equal(today, calendar[0].Date);
        Assert.Equal(3, calendar[0].Count);
        Assert.Equal(today.AddMonths(-12), result.Value.SubmissionCalendarRangeStart);
        Assert.Equal(today, result.Value.SubmissionCalendarRangeEnd);
    }

    [Fact]
    public async Task Handle_PublicProfile_NotOwner_StillFetchesSections()
    {
        var profile = CreateBaseProfile(Guid.NewGuid(), isPrivate: false);
        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", null),
            CancellationToken.None
        );

        Assert.NotNull(result.Value.GameModeStats);
    }

    [Fact]
    public async Task Handle_MultiplayerMode_ComputesWinsLossesDrawsAndBestScore()
    {
        var userId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        var mode = new GameMode("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid());

        var loss = CreateCompletedGame(mode.Id, (userId, 3), (opponentId, 5));
        var draw = CreateCompletedGame(mode.Id, (userId, 5), (opponentId, 5));
        var win = CreateCompletedGame(mode.Id, (userId, 5), (opponentId, 2));

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[mode]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[loss, draw, win]);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", userId),
            CancellationToken.None
        );

        var stat = result.Value.GameModeStats!.Single();
        Assert.Equal(3, stat.GamesPlayed);
        Assert.Equal(1, stat.Wins);
        Assert.Equal(1, stat.Losses);
        Assert.Equal(1, stat.Draws);
        Assert.Equal(5, stat.BestScore);
        Assert.True(stat.HasOpponents);
    }

    [Fact]
    public async Task Handle_SoloMode_OnlyTracksBestScoreWithoutWinLossDraw()
    {
        var userId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        var soloMode = new GameMode("solo-rush", "Solo Rush", "Beat your best", true, 1, 1, Guid.NewGuid());

        var game = CreateCompletedGame(soloMode.Id, (userId, 7));

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[soloMode]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[game]);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", userId),
            CancellationToken.None
        );

        var stat = result.Value.GameModeStats!.Single();
        Assert.False(stat.HasOpponents);
        Assert.Equal(7, stat.BestScore);
        Assert.Equal(0, stat.Wins);
        Assert.Equal(0, stat.Losses);
        Assert.Equal(0, stat.Draws);
        Assert.Equal(1, stat.GamesPlayed);
    }

    [Fact]
    public async Task Handle_DeactivatedGameMode_StillIncludedWhenLookupSucceeds()
    {
        var userId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        var retiredMode = new GameMode("retired", "Retired Mode", "No longer offered", true, 2, 2, Guid.NewGuid());
        retiredMode.Deactivate();

        var game = CreateCompletedGame(retiredMode.Id, (userId, 4), (Guid.NewGuid(), 1));

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[game]);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(retiredMode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retiredMode);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", userId),
            CancellationToken.None
        );

        Assert.Equal("Retired Mode", result.Value.GameModeStats!.Single().GameModeName);
    }

    [Fact]
    public async Task Handle_UnknownGameMode_ExcludedFromStatsAndRecentGames()
    {
        var userId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        var game = CreateCompletedGame(Guid.NewGuid(), (userId, 4), (Guid.NewGuid(), 1));

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[game]);
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(game.GameModeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", userId),
            CancellationToken.None
        );

        Assert.NotNull(result.Value.GameModeStats);
        Assert.Empty(result.Value.GameModeStats);
        Assert.NotNull(result.Value.RecentGames);
        Assert.Empty(result.Value.RecentGames);
    }

    [Fact]
    public async Task Handle_RecentGames_MapsParticipantsOrderedByScoreDescendingWithUnknownFallback()
    {
        var userId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var profile = CreateBaseProfile(userId, isPrivate: false);
        var mode = new GameMode("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid());
        var game = CreateCompletedGame(mode.Id, (userId, 2), (opponentId, 6));

        _userReadRepository
            .Setup(x => x.FindProfileByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _gameReadRepository
            .Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<GameMode>)[mode]);
        _gameReadRepository
            .Setup(x => x.GetCompletedGamesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Game>)[game]);
        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, UserDto>)
                    new Dictionary<Guid, UserDto>
                    {
                        [userId] = new UserDto(
                            userId,
                            "auth0|a",
                            "alice",
                            null,
                            null,
                            false,
                            null,
                            DateTime.UtcNow,
                            null,
                            []
                        ),
                    }
            );

        Result<UserProfileDto> result = await _handler.Handle(
            new GetUserProfileByUsernameQuery("alice", userId),
            CancellationToken.None
        );

        var recentGame = result.Value.RecentGames!.Single();
        Assert.Equal("Unknown", recentGame.Participants[0].Username);
        Assert.Equal(6, recentGame.Participants[0].Score);
        Assert.Equal("alice", recentGame.Participants[1].Username);
        Assert.Equal(2, recentGame.Participants[1].Score);
    }
}