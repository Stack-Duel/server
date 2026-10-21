using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using LanguageVersionResolver = StackDuel.Application.Languages.LanguageVersionResolver;

namespace StackDuel.Application.Tests.Languages;

public class LanguageVersionResolverTests
{
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private LanguageVersionResolver _resolver = null!;

    public LanguageVersionResolverTests()
    {
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _resolver = new LanguageVersionResolver(_languageReadRepository.Object);
    }

    private static Language CreateLanguage(string name, string slug, params string[] versions)
    {
        var language = new Language(new LanguageName(name), new LanguageSlug(slug), Guid.NewGuid());

        foreach (string version in versions)
            language.AddVersion(new LanguageVersion(version), new Judge0Id(1));

        return language;
    }

    private void GivenLanguages(params Language[] languages) =>
        _languageReadRepository
            .Setup(x => x.GetActiveLanguagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(languages);

    [Fact]
    public async Task ResolveAsync_ExplicitId_IsReturnedWithoutLookup()
    {
        Guid languageVersionId = Guid.NewGuid();

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(languageVersionId, null, null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(languageVersionId, result.Value);
        _languageReadRepository.Verify(x => x.GetActiveLanguagesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_NeitherIdNorLanguage_IsInvalid()
    {
        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, null, null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_SlugWithSingleActiveVersion_NeedsNoVersionLabel()
    {
        Language python = CreateLanguage("Python", "python", "3.13.2");
        GivenLanguages(python);

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "python", null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(python.Versions.Single().Id, result.Value);
    }

    [Fact]
    public async Task ResolveAsync_DisplayNameIsAcceptedCaseInsensitively()
    {
        Language python = CreateLanguage("Python", "python", "3.13.2");
        GivenLanguages(python);

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "PYTHON", null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResolveAsync_UnknownLanguage_ListsTheAvailableSlugs()
    {
        GivenLanguages(CreateLanguage("Python", "python", "3.13.2"), CreateLanguage("Java", "java", "JDK 17.0.6"));

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "py", null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        string message = string.Join(" ", result.ValidationErrors.Select(error => error.ErrorMessage));
        Assert.Contains("java", message);
        Assert.Contains("python", message);
    }

    [Fact]
    public async Task ResolveAsync_SeveralActiveVersionsAndNoLabel_IsInvalidAndListsThem()
    {
        GivenLanguages(CreateLanguage("JavaScript", "javascript", "Node.js 22.08.0", "Node.js 18.0.0"));

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "javascript", null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        string message = string.Join(" ", result.ValidationErrors.Select(error => error.ErrorMessage));
        Assert.Contains("Node.js 22.08.0", message);
        Assert.Contains("Node.js 18.0.0", message);
    }

    [Fact]
    public async Task ResolveAsync_MatchingVersionLabel_SelectsThatVersion()
    {
        Language javascript = CreateLanguage("JavaScript", "javascript", "Node.js 22.08.0", "Node.js 18.0.0");
        GivenLanguages(javascript);

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "javascript", "Node.js 18.0.0"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(javascript.Versions.Single(v => v.Version.Value == "Node.js 18.0.0").Id, result.Value);
    }

    [Fact]
    public async Task ResolveAsync_UnknownVersionLabel_IsInvalidAndListsTheRealOnes()
    {
        GivenLanguages(CreateLanguage("Python", "python", "3.13.2"));

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "python", "2.7"),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        string message = string.Join(" ", result.ValidationErrors.Select(error => error.ErrorMessage));
        Assert.Contains("3.13.2", message);
    }

    [Fact]
    public async Task ResolveAsync_DeprecatedVersionIsNotSelectable()
    {
        Language python = CreateLanguage("Python", "python", "3.13.2", "2.7");
        python.DeprecateVersion(python.Versions.Single(v => v.Version.Value == "2.7").Id);
        GivenLanguages(python);

        Result<Guid> deprecated = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "python", "2.7"),
            CancellationToken.None
        );
        Result<Guid> remaining = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "python", null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, deprecated.Status);
        Assert.True(remaining.IsSuccess);
        Assert.Equal(python.Versions.Single(v => v.Version.Value == "3.13.2").Id, remaining.Value);
    }

    [Fact]
    public async Task ResolveAsync_LanguageWithNoActiveVersions_IsInvalid()
    {
        GivenLanguages(CreateLanguage("Python", "python"));

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "python", null),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_SurroundingWhitespaceIsIgnored()
    {
        GivenLanguages(CreateLanguage("Python", "python", "3.13.2"));

        Result<Guid> result = await _resolver.ResolveAsync(
            new LanguageVersionReference(null, "  python  ", "  3.13.2  "),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }
}