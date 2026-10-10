using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;
using LanguageEntity = StackDuel.Domain.Languages.Entities.Language;

namespace StackDuel.Domain.Tests.Language.Entities;

public class LanguageTests
{
    private static readonly LanguageName ValidName = new("Python");
    private static readonly LanguageSlug ValidSlug = new("python");
    private static readonly LanguageVersion ValidVersion = new("3.11");
    private static readonly Judge0Id ValidJudge0Id = new(109);

    private static LanguageEntity CreateLanguage() => new(ValidName, ValidSlug, Guid.NewGuid());

    [Fact]
    public void Activate_WhenInactive_SetsStatusToActive()
    {
        var language = CreateLanguage();
        language.Deactivate();

        language.Activate();

        Assert.Equal(LanguageStatus.Active, language.Status);
    }

    [Fact]
    public void AddVersion_AddsToVersionsCollection()
    {
        var language = CreateLanguage();

        language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.Single(language.Versions);
    }

    [Fact]
    public void AddVersion_MultipleVersions_AllAdded()
    {
        var language = CreateLanguage();

        language.AddVersion(new LanguageVersion("3.10"), new Judge0Id(100));
        language.AddVersion(new LanguageVersion("3.11"), new Judge0Id(109));

        Assert.Equal(2, language.Versions.Count);
    }

    [Fact]
    public void AddVersion_ReturnsEntryWithActiveStatus()
    {
        var language = CreateLanguage();

        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.True(entry.IsActive);
        Assert.Equal(ValidVersion, entry.Version);
        Assert.Equal(ValidJudge0Id, entry.Judge0Id);
    }

    [Fact]
    public void Constructor_SetsNameAndSlug()
    {
        var language = CreateLanguage();

        Assert.Equal(ValidName, language.Name);
        Assert.Equal(ValidSlug, language.Slug);
    }

    [Fact]
    public void Constructor_SetsStatusToActive()
    {
        var language = CreateLanguage();

        Assert.Equal(LanguageStatus.Active, language.Status);
    }

    [Fact]
    public void Constructor_VersionsIsEmpty()
    {
        var language = CreateLanguage();

        Assert.Empty(language.Versions);
    }

    [Fact]
    public void Deactivate_SetsStatusToInactive()
    {
        var language = CreateLanguage();

        language.Deactivate();

        Assert.Equal(LanguageStatus.Inactive, language.Status);
    }

    [Fact]
    public void DeprecateVersion_DoesNotRemoveVersion()
    {
        var language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        language.DeprecateVersion(entry.Id);

        Assert.Single(language.Versions);
    }

    [Fact]
    public void DeprecateVersion_SetsVersionToDeprecated()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        language.DeprecateVersion(entry.Id);

        Assert.Equal(LanguageVersionStatus.Deprecated, entry.Status);
    }

    [Fact]
    public void DeprecateVersion_UnknownVersionId_ThrowsLanguageVersionNotFoundException()
    {
        var language = CreateLanguage();

        Assert.Throws<LanguageVersionNotFoundException>(() => language.DeprecateVersion(Guid.NewGuid()));
    }

    [Fact]
    public void IsActive_WhenActive_IsTrue()
    {
        var language = CreateLanguage();

        Assert.True(language.IsActive);
    }

    [Fact]
    public void IsActive_WhenInactive_IsFalse()
    {
        var language = CreateLanguage();
        language.Deactivate();

        Assert.False(language.IsActive);
    }
}