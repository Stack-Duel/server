using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Tracks;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Tracks.Entities;
using GetTracksHandler = StackDuel.Application.Queries.Tracks.GetTracks.GetTracksHandler;
using GetTracksQuery = StackDuel.Application.Queries.Tracks.GetTracks.GetTracksQuery;

namespace StackDuel.Application.Tests.Queries.Tracks.GetTracks;

public class GetTracksHandlerTests
{
    private Mock<ITrackReadRepository> _trackReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetTracksHandler _handler = null!;

    public GetTracksHandlerTests()
    {
        _trackReadRepository = new Mock<ITrackReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetTracksHandler(_trackReadRepository.Object, _languageReadRepository.Object);
    }

    [Fact]
    public async Task Handle_NoTracks_ReturnsEmptyList()
    {
        _trackReadRepository.Setup(x => x.GetActiveTracksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _languageReadRepository
            .Setup(x =>
                x.GetActiveLanguagesByTrackIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        Result<IReadOnlyList<TrackDto>> result = await _handler.Handle(new GetTracksQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_Success_GroupsLanguagesUnderTheirOwnTrackOnly()
    {
        var frontendTrack = new Track("frontend", "Frontend");
        var sqlTrack = new Track("sql", "SQL");
        var react = new Language(new LanguageName("React"), new LanguageSlug("react"), frontendTrack.Id);
        var angular = new Language(new LanguageName("Angular"), new LanguageSlug("angular"), frontendTrack.Id);
        var sqlite = new Language(new LanguageName("SQLite"), new LanguageSlug("sqlite"), sqlTrack.Id);

        _trackReadRepository
            .Setup(x => x.GetActiveTracksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([frontendTrack, sqlTrack]);
        _languageReadRepository
            .Setup(x =>
                x.GetActiveLanguagesByTrackIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([react, angular, sqlite]);

        Result<IReadOnlyList<TrackDto>> result = await _handler.Handle(new GetTracksQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        TrackDto frontendDto = result.Value.Single(t => t.Key == "frontend");
        TrackDto sqlDto = result.Value.Single(t => t.Key == "sql");

        Assert.Equivalent(new[] { "React", "Angular" }, frontendDto.Languages.Select(l => l.Name), strict: true);
        Assert.Equivalent(new[] { "SQLite" }, sqlDto.Languages.Select(l => l.Name), strict: true);
    }
}