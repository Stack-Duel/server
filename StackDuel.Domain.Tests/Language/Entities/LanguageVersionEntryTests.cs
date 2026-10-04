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

    [Test]
    public void Deprecate_SetsIsActiveToFalse()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        entry.Deprecate();

        Assert.That(entry.IsActive, Is.False);
    }

    [Test]
    public void Deprecate_SetsStatusToDeprecated()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        entry.Deprecate();

        Assert.That(entry.Status, Is.EqualTo(LanguageVersionStatus.Deprecated));
    }

    [Test]
    public void InitialStatus_IsActive()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.That(entry.Status, Is.EqualTo(LanguageVersionStatus.Active));
    }

    [Test]
    public void IsActive_WhenActive_IsTrue()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.That(entry.IsActive, Is.True);
    }

    [Test]
    public void Version_SetCorrectly()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.That(entry.Version, Is.EqualTo(ValidVersion));
    }
}