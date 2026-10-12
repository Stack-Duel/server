using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Languages;
using StackDuel.Application.Tracks;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Tracks.Entities;
using CreateGameCommand = StackDuel.Application.Commands.Games.CreateGame.CreateGameCommand;
using CreateGameHandler = StackDuel.Application.Commands.Games.CreateGame.CreateGameHandler;
using CreateGameValidator = StackDuel.Application.Commands.Games.CreateGame.CreateGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.CreateGame;

public class CreateGameHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private UserContext _userContext = null!;
    private CreateGameHandler _handler = null!;

    public CreateGameHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _userContext = new UserContext();

        _handler = new CreateGameHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _trackReadRepository.Object,
            _languageReadRepository.Object,
            _userContext,
            new CreateGameValidator()
        );
    }

    private static GameMode CreateDuelMode(int durationSeconds = 600)
    {
        var mode = new GameMode("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid());
        mode.AddTimeOption(durationSeconds);
        return mode;
    }

    private static Track CreateGeneralPurposeTrack(bool allowsLanguageSelection = true)
    {
        var track = new Track("general-purpose", "General Purpose");
        if (!allowsLanguageSelection)
            track.DisableLanguageSelection();
        return track;
    }

    private static Track CreateSqlTrack() => new("sql", "SQL");

    private static Language CreateLanguage(string name, string slug, Guid trackId) =>
        new(new LanguageName(name), new LanguageSlug(slug), trackId);

    private void StubTracksFound(params Track[] tracks) =>
        _trackReadRepository
            .Setup(x => x.FindByKeysAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracks);

    private void StubLanguagesFound(params Language[] languages) =>
        _languageReadRepository
            .Setup(x =>
                x.GetActiveLanguagesByTrackIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(languages);

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotQueryGameMode()
    {
        var command = new CreateGameCommand(string.Empty, [], 0, Guid.Empty);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameReadRepository.Verify(
            x => x.FindGameModeByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_GameModeNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMode?)null);

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection("general-purpose", [Guid.NewGuid()])],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_TrackNotFound_ReturnsNotFound()
    {
        var mode = CreateDuelMode();
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound();

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection("frontend", [Guid.NewGuid()])],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_SomeTracksNotFound_ReturnsNotFound()
    {
        var mode = CreateDuelMode();
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(CreateGeneralPurposeTrack());

        var command = new CreateGameCommand(
            "duel",
            [
                new TrackLanguageSelection("general-purpose", [Guid.NewGuid()]),
                new TrackLanguageSelection("frontend", [Guid.NewGuid()]),
            ],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_LanguageNotAvailableForTrack_ReturnsNotFound()
    {
        var mode = CreateDuelMode();
        var track = CreateGeneralPurposeTrack();
        var otherTrackLanguage = CreateLanguage("React", "react", Guid.NewGuid());
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(otherTrackLanguage);

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(track.Key, [Guid.NewGuid()])],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_TrackDisallowsLanguageSelection_PartialLanguageSet_ReturnsInvalid()
    {
        var mode = CreateDuelMode();
        var track = CreateGeneralPurposeTrack(allowsLanguageSelection: false);
        var javascript = CreateLanguage("JavaScript", "javascript", track.Id);
        var python = CreateLanguage("Python", "python", track.Id);
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(javascript, python);

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(track.Key, [javascript.Id])],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameWriteRepository.Verify(x => x.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TrackDisallowsLanguageSelection_AllActiveLanguagesIncluded_CreatesGame()
    {
        var mode = CreateDuelMode(600);
        var track = CreateGeneralPurposeTrack(allowsLanguageSelection: false);
        var javascript = CreateLanguage("JavaScript", "javascript", track.Id);
        var python = CreateLanguage("Python", "python", track.Id);
        var userId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(javascript, python);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        Game? addedGame = null;
        _gameWriteRepository
            .Setup(x => x.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Callback<Game, CancellationToken>((g, _) => addedGame = g)
            .Returns(Task.CompletedTask);

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(track.Key, [javascript.Id, python.Id])],
            600,
            userId
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equivalent(
            new[] { javascript.Id, python.Id },
            addedGame!.Tracks.Single(t => t.TrackId == track.Id).Languages.Select(l => l.LanguageId),
            strict: true
        );
    }

    [Fact]
    public async Task Handle_UserLacksRequiredPermission_ReturnsForbidden()
    {
        var mode = CreateDuelMode();
        var track = CreateGeneralPurposeTrack();
        var language = CreateLanguage("JavaScript", "javascript", track.Id);
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(language);
        _userContext.Permissions = [];

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(track.Key, [language.Id])],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_UnmappedGameModeKey_ReturnsForbiddenWithoutCheckingPermission()
    {
        var mode = new GameMode("mystery", "Mystery", "Unmapped mode", true, 1, 1, Guid.NewGuid());
        mode.AddTimeOption(600);
        var track = CreateGeneralPurposeTrack();
        var language = CreateLanguage("JavaScript", "javascript", track.Id);
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("mystery", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(language);

        var command = new CreateGameCommand(
            "mystery",
            [new TrackLanguageSelection(track.Key, [language.Id])],
            600,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_DurationNotOfferedByGameMode_ReturnsInvalid()
    {
        var mode = CreateDuelMode(600);
        var track = CreateGeneralPurposeTrack();
        var language = CreateLanguage("JavaScript", "javascript", track.Id);
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(language);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(track.Key, [language.Id])],
            900,
            Guid.NewGuid()
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameWriteRepository.Verify(x => x.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesGameAndReturnsSuccess()
    {
        var mode = CreateDuelMode(600);
        var track = CreateGeneralPurposeTrack();
        var language = CreateLanguage("JavaScript", "javascript", track.Id);
        var userId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(track);
        StubLanguagesFound(language);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        Game? addedGame = null;
        _gameWriteRepository
            .Setup(x => x.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Callback<Game, CancellationToken>((g, _) => addedGame = g)
            .Returns(Task.CompletedTask);

        var command = new CreateGameCommand(
            "duel",
            [new TrackLanguageSelection(track.Key, [language.Id])],
            600,
            userId
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(addedGame);
        Assert.Equal(mode.Id, addedGame!.GameModeId);
        Assert.Contains(track.Id, addedGame.Tracks.Select(t => t.TrackId));
        Assert.Equivalent(
            new[] { language.Id },
            addedGame.Tracks.Single(t => t.TrackId == track.Id).Languages.Select(l => l.LanguageId),
            strict: true
        );
        Assert.Contains(userId, addedGame.Participants.Select(p => p.UserId));
        Assert.Equal(addedGame.Id, result.Value);
    }

    [Fact]
    public async Task Handle_ValidRequest_WithMultipleTracks_CreatesGameWithAllTracks()
    {
        var mode = CreateDuelMode(600);
        var generalPurposeTrack = CreateGeneralPurposeTrack();
        var sqlTrack = CreateSqlTrack();
        var generalPurposeLanguage = CreateLanguage("JavaScript", "javascript", generalPurposeTrack.Id);
        var sqlLanguage = CreateLanguage("SQLite", "sqlite", sqlTrack.Id);
        var userId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameModeByKeyAsync("duel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);
        StubTracksFound(generalPurposeTrack, sqlTrack);
        StubLanguagesFound(generalPurposeLanguage, sqlLanguage);
        _userContext.Permissions = [WellKnownAuthorization.PlayDuelPermission];

        Game? addedGame = null;
        _gameWriteRepository
            .Setup(x => x.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Callback<Game, CancellationToken>((g, _) => addedGame = g)
            .Returns(Task.CompletedTask);

        var command = new CreateGameCommand(
            "duel",
            [
                new TrackLanguageSelection(generalPurposeTrack.Key, [generalPurposeLanguage.Id]),
                new TrackLanguageSelection(sqlTrack.Key, [sqlLanguage.Id]),
            ],
            600,
            userId
        );
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equivalent(
            new[] { generalPurposeTrack.Id, sqlTrack.Id },
            addedGame!.Tracks.Select(t => t.TrackId),
            strict: true
        );
    }
}