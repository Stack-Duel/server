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

    [Test]
    public void Activate_WhenInactive_SetsStatusToActive()
    {
        var language = CreateLanguage();
        language.Deactivate();

        language.Activate();

        Assert.That(language.Status, Is.EqualTo(LanguageStatus.Active));
    }

    [Test]
    public void AddVersion_AddsToVersionsCollection()
    {
        var language = CreateLanguage();

        language.AddVersion(ValidVersion, ValidJudge0Id);

        Assert.That(language.Versions, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddVersion_MultipleVersions_AllAdded()
    {
        var language = CreateLanguage();

        language.AddVersion(new LanguageVersion("3.10"), new Judge0Id(100));
        language.AddVersion(new LanguageVersion("3.11"), new Judge0Id(109));

        Assert.That(language.Versions, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddVersion_ReturnsEntryWithActiveStatus()
    {
        var language = CreateLanguage();

        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.IsActive, Is.True);
            Assert.That(entry.Version, Is.EqualTo(ValidVersion));
            Assert.That(entry.Judge0Id, Is.EqualTo(ValidJudge0Id));
        }
    }

    [Test]
    public void Constructor_SetsNameAndSlug()
    {
        var language = CreateLanguage();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(language.Name, Is.EqualTo(ValidName));
            Assert.That(language.Slug, Is.EqualTo(ValidSlug));
        }
    }

    [Test]
    public void Constructor_SetsStatusToActive()
    {
        var language = CreateLanguage();

        Assert.That(language.Status, Is.EqualTo(LanguageStatus.Active));
    }

    [Test]
    public void Constructor_VersionsIsEmpty()
    {
        var language = CreateLanguage();

        Assert.That(language.Versions, Is.Empty);
    }

    [Test]
    public void Deactivate_SetsStatusToInactive()
    {
        var language = CreateLanguage();

        language.Deactivate();

        Assert.That(language.Status, Is.EqualTo(LanguageStatus.Inactive));
    }

    [Test]
    public void DeprecateVersion_DoesNotRemoveVersion()
    {
        var language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        language.DeprecateVersion(entry.Id);

        Assert.That(language.Versions, Has.Count.EqualTo(1));
    }

    [Test]
    public void DeprecateVersion_SetsVersionToDeprecated()
    {
        LanguageEntity language = CreateLanguage();
        LanguageVersionEntry entry = language.AddVersion(ValidVersion, ValidJudge0Id);

        language.DeprecateVersion(entry.Id);

        Assert.That(entry.Status, Is.EqualTo(LanguageVersionStatus.Deprecated));
    }

    [Test]
    public void DeprecateVersion_UnknownVersionId_ThrowsLanguageVersionNotFoundException()
    {
        var language = CreateLanguage();

        Assert.Throws<LanguageVersionNotFoundException>(() => language.DeprecateVersion(Guid.NewGuid()));
    }

    [Test]
    public void IsActive_WhenActive_IsTrue()
    {
        var language = CreateLanguage();

        Assert.That(language.IsActive, Is.True);
    }

    [Test]
    public void IsActive_WhenInactive_IsFalse()
    {
        var language = CreateLanguage();
        language.Deactivate();

        Assert.That(language.IsActive, Is.False);
    }
}