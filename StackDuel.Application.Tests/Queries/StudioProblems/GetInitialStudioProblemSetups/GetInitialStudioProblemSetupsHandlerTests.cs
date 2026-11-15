using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Queries.StudioProblems.GetInitialStudioProblemSetups;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Tests.Queries.StudioProblems.GetInitialStudioProblemSetups;

public class GetInitialStudioProblemSetupsHandlerTests
{
    private readonly Mock<ITrackReadRepository> _trackReadRepository = new();
    private readonly Mock<ILanguageReadRepository> _languageReadRepository = new();
    private readonly GetInitialStudioProblemSetupsHandler _handler;

    public GetInitialStudioProblemSetupsHandlerTests()
    {
        _handler = new GetInitialStudioProblemSetupsHandler(_trackReadRepository.Object, _languageReadRepository.Object);
    }

    [Fact]
    public async Task Handle_NoActiveTracks_ReturnsEmptyAvailableTracks()
    {
        _trackReadRepository.Setup(x => x.GetActiveTracksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _languageReadRepository
            .Setup(x =>
                x.GetActiveLanguagesByTrackIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        Result<InitialStudioProblemSetupDto> result = await _handler.Handle(
            new GetInitialStudioProblemSetupsQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AvailableTracks);
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

        Result<InitialStudioProblemSetupDto> result = await _handler.Handle(
            new GetInitialStudioProblemSetupsQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var frontendDto = result.Value.AvailableTracks.Single(t => t.Key == "frontend");
        var sqlDto = result.Value.AvailableTracks.Single(t => t.Key == "sql");

        Assert.Equivalent(new[] { "React", "Angular" }, frontendDto.Languages.Select(l => l.Name), strict: true);
        Assert.Equivalent(new[] { "SQLite" }, sqlDto.Languages.Select(l => l.Name), strict: true);

        _languageReadRepository.Verify(x =>
            x.GetActiveLanguagesByTrackIdsAsync(
                It.Is<IEnumerable<Guid>>(trackIds =>
                    trackIds.SequenceEqual(new[] { frontendTrack.Id, sqlTrack.Id })
                ),
                It.IsAny<CancellationToken>()
            )
        );
    }
}