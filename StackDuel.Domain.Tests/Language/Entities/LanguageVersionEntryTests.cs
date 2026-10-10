using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Languages.ValueObjects;
using LanguageEntity = StackDuel.Domain.Languages.Entities.Language;

namespace StackDuel.Domain.Tests.Language.Entities;

public class LanguageVersionEntryTests
{
    private static readonly LanguageName ValidName = new("Python");
    private static readonly LanguageSlug ValidSlug = new("python");
    private static readonly LanguageVersion ValidVersion = new("3.11");
    private static readonly Judge0Id ValidJudge0Id = new(109);

    private static LanguageEntity CreateLanguage() => new(ValidName, ValidSlug, Guid.NewGuid());

    [Fact]
    public void Deprecate_SetsIsActiveToFalse()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        entry.Deprecate();

        Assert.False(entry.IsActive);
    }

    [Fact]
    public void Deprecate_SetsStatusToDeprecated()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        entry.Deprecate();

        Assert.Equal(LanguageVersionStatus.Deprecated, entry.Status);
    }

    [Fact]
    public void InitialStatus_IsActive()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.Equal(LanguageVersionStatus.Active, entry.Status);
    }

    [Fact]
    public void IsActive_WhenActive_IsTrue()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.True(entry.IsActive);
    }

    [Fact]
    public void Version_SetCorrectly()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.Equal(ValidVersion, entry.Version);
    }
}