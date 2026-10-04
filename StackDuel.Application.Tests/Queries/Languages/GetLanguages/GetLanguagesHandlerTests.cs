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

    [SetUp]
    public void SetUp()
    {
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetLanguagesHandler(_languageReadRepository.Object);
    }

    [Test]
    public async Task Handle_NoLanguages_ReturnsEmptyList()
    {
        _languageReadRepository.Setup(x => x.GetActiveLanguagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<LanguageDto>> result = await _handler.Handle(
            new GetLanguagesQuery(),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
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

        Assert.That(result.IsSuccess, Is.True);
        LanguageDto dto = result.Value.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(language.Id));
            Assert.That(dto.Name, Is.EqualTo("Python"));
            Assert.That(dto.Versions, Has.Count.EqualTo(1));
            Assert.That(dto.Versions.Single().Id, Is.EqualTo(activeVersion.Id));
            Assert.That(dto.Versions.Single().Version, Is.EqualTo("3.12"));
        });
    }
}