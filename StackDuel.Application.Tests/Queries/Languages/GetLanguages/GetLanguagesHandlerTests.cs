using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Languages.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using GetLanguagesHandler = StackDuel.Application.Queries.Languages.GetLanguages.GetLanguagesHandler;
using GetLanguagesQuery = StackDuel.Application.Queries.Languages.GetLanguages.GetLanguagesQuery;

namespace StackDuel.Application.Tests.Queries.Languages.GetLanguages;

public class GetLanguagesHandlerTests
{
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetLanguagesHandler _handler = null!;

    public GetLanguagesHandlerTests()
    {
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetLanguagesHandler(_languageReadRepository.Object);
    }

    [Fact]
    public async Task Handle_NoLanguages_ReturnsEmptyList()
    {
        _languageReadRepository.Setup(x => x.GetActiveLanguagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<LanguageDto>> result = await _handler.Handle(
            new GetLanguagesQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_Success_MapsNameAndOnlyActiveVersions()
    {
        var language = new Language(new LanguageName("Python"), new LanguageSlug("python"), Guid.NewGuid());
        var activeVersion = language.AddVersion(new LanguageVersion("3.12"), new Judge0Id(1));
        var deprecatedVersion = language.AddVersion(new LanguageVersion("2.7"), new Judge0Id(2));
        deprecatedVersion.Deprecate();

        _languageReadRepository
            .Setup(x => x.GetActiveLanguagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        Result<IReadOnlyList<LanguageDto>> result = await _handler.Handle(
            new GetLanguagesQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        LanguageDto dto = result.Value.Single();
        Assert.Equal(language.Id, dto.Id);
        Assert.Equal("Python", dto.Name);
        Assert.Single(dto.Versions);
        Assert.Equal(activeVersion.Id, dto.Versions.Single().Id);
        Assert.Equal("3.12", dto.Versions.Single().Version);
    }
}