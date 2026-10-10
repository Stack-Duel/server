using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignTests
{
    private static readonly int[] SequentialSortOrders = [0, 1, 2];

    private static Campaign CreateCampaign(
        string slug = "intro-to-algorithms",
        CampaignDifficulty difficulty = CampaignDifficulty.Beginner
    ) => new(slug, "Intro to Algorithms", "Start here.", difficulty);

    [Test]
    public void Constructor_SetsDetails()
    {
        Campaign campaign = CreateCampaign(difficulty: CampaignDifficulty.Advanced);

        Assert.Multiple(() =>
        {
            Assert.That(campaign.Slug, Is.EqualTo("intro-to-algorithms"));
            Assert.That(campaign.Title, Is.EqualTo("Intro to Algorithms"));
            Assert.That(campaign.Description, Is.EqualTo("Start here."));
            Assert.That(campaign.Difficulty, Is.EqualTo(CampaignDifficulty.Advanced));
        });
    }

    [Test]
    public void Constructor_StartsAsDraftWithNoPublishDate()
    {
        Campaign campaign = CreateCampaign();

        Assert.Multiple(() =>
        {
            Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Draft));
            Assert.That(campaign.PublishedAt, Is.Null);
        });
    }

    [Test]
    public void Constructor_StartsWithNoIconSortOrderZeroAndNoChildren()
    {
        Campaign campaign = CreateCampaign();

        Assert.Multiple(() =>
        {
            Assert.That(campaign.IconKey, Is.Null);
            Assert.That(campaign.SortOrder, Is.Zero);
            Assert.That(campaign.Modules, Is.Empty);
            Assert.That(campaign.Prerequisites, Is.Empty);
        });
    }

    [Test]
    public void Constructor_StampsCreatedAt()
    {
        DateTime before = DateTime.UtcNow;
        Assert.That(CreateCampaign().CreatedAt, Is.GreaterThanOrEqualTo(before));
    }

    [TestCase("  Intro-To-Algorithms  ", "intro-to-algorithms", Description = "trimmed and lowercased")]
    [TestCase("ALGO", "algo", Description = "lowercased")]
    public void Constructor_NormalizesSlug(string slug, string expected)
    {
        Assert.That(CreateCampaign(slug).Slug, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankSlug_Throws(string? slug)
    {
        Assert.Throws<ArgumentException>(() => CreateCampaign(slug!));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new Campaign("slug", title!, "desc", CampaignDifficulty.Beginner));
    }

    [Test]
    public void Constructor_TrimsTitle()
    {
        Campaign campaign = new("slug", "  Padded  ", "desc", CampaignDifficulty.Beginner);
        Assert.That(campaign.Title, Is.EqualTo("Padded"));
    }

    [Test]
    public void Constructor_NullDescription_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Campaign("slug", "Title", null!, CampaignDifficulty.Beginner));
    }

    [Test]
    public void Constructor_EmptyDescription_IsAllowed()
    {
        Campaign campaign = new("slug", "Title", "", CampaignDifficulty.Beginner);
        Assert.That(campaign.Description, Is.Empty);
    }

    [Test]
    public void UpdateDetails_ReplacesTitleDescriptionDifficultyAndIcon()
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Renamed", "New description.", CampaignDifficulty.Expert, "icon-renamed");

        Assert.Multiple(() =>
        {
            Assert.That(campaign.Title, Is.EqualTo("Renamed"));
            Assert.That(campaign.Description, Is.EqualTo("New description."));
            Assert.That(campaign.Difficulty, Is.EqualTo(CampaignDifficulty.Expert));
            Assert.That(campaign.IconKey, Is.EqualTo("icon-renamed"));
        });
    }

    [Test]
    public void UpdateDetails_TrimsIconKey()
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Title", "desc", CampaignDifficulty.Beginner, "  icon  ");

        Assert.That(campaign.IconKey, Is.EqualTo("icon"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDetails_BlankIconKey_IsStoredAsNull(string? iconKey)
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Title", "desc", CampaignDifficulty.Beginner, iconKey);

        Assert.That(campaign.IconKey, Is.Null);
    }

    [Test]
    public void UpdateDetails_DoesNotChangeTheSlug()
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Renamed", "desc", CampaignDifficulty.Expert, null);

        Assert.That(campaign.Slug, Is.EqualTo("intro-to-algorithms"));
    }

    [Test]
    public void Publish_FromDraft_SetsStatusAndPublishedAt()
    {
        Campaign campaign = CreateCampaign();
        DateTime before = DateTime.UtcNow;

        campaign.Publish();

        Assert.Multiple(() =>
        {
            Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Published));
            Assert.That(campaign.PublishedAt, Is.GreaterThanOrEqualTo(before));
        });
    }

    [Test]
    public void Publish_AlreadyPublished_Throws()
    {
        Campaign campaign = CreateCampaign();
        campaign.Publish();

        Assert.Throws<InvalidOperationException>(campaign.Publish);
    }

    [Test]
    public void Publish_Archived_Throws()
    {
        Campaign campaign = CreateCampaign();
        campaign.Archive();

        Assert.Throws<InvalidOperationException>(campaign.Publish);
    }

    [Test]
    public void Archive_FromDraft_SetsStatus()
    {
        Campaign campaign = CreateCampaign();

        campaign.Archive();

        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Archived));
    }

    [Test]
    public void Archive_FromPublished_SetsStatusAndKeepsPublishedAt()
    {
        Campaign campaign = CreateCampaign();
        campaign.Publish();
        DateTime? publishedAt = campaign.PublishedAt;

        campaign.Archive();

        Assert.Multiple(() =>
        {
            Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Archived));
            Assert.That(campaign.PublishedAt, Is.EqualTo(publishedAt));
        });
    }

    [Test]
    public void Archive_AlreadyArchived_Throws()
    {
        Campaign campaign = CreateCampaign();
        campaign.Archive();

        Assert.Throws<InvalidOperationException>(campaign.Archive);
    }

    [Test]
    public void SetSortOrder_StoresValue()
    {
        Campaign campaign = CreateCampaign();

        campaign.SetSortOrder(7);

        Assert.That(campaign.SortOrder, Is.EqualTo(7));
    }

    [Test]
    public void AddModule_AppendsModuleOwnedByTheCampaign()
    {
        Campaign campaign = CreateCampaign();

        CampaignModule module = campaign.AddModule("Module 1", "First module.");

        Assert.Multiple(() =>
        {
            Assert.That(campaign.Modules, Has.Count.EqualTo(1));
            Assert.That(module.CampaignId, Is.EqualTo(campaign.Id));
            Assert.That(module.Title, Is.EqualTo("Module 1"));
        });
    }

    [Test]
    public void AddModule_AssignsSequentialSortOrders()
    {
        Campaign campaign = CreateCampaign();

        CampaignModule first = campaign.AddModule("Module 1", "");
        CampaignModule second = campaign.AddModule("Module 2", "");
        CampaignModule third = campaign.AddModule("Module 3", "");

        Assert.That(new[] { first.SortOrder, second.SortOrder, third.SortOrder }, Is.EqualTo(SequentialSortOrders));
    }

    [Test]
    public void SetPrerequisites_AddsOnePrerequisitePerId()
    {
        Campaign campaign = CreateCampaign();
        Guid requiredA = Guid.NewGuid();
        Guid requiredB = Guid.NewGuid();

        campaign.SetPrerequisites([requiredA, requiredB]);

        Assert.That(
            campaign.Prerequisites.Select(p => p.RequiredCampaignId),
            Is.EquivalentTo(new[] { requiredA, requiredB })
        );
        Assert.That(campaign.Prerequisites.Select(p => p.CampaignId), Has.All.EqualTo(campaign.Id));
    }

    [Test]
    public void SetPrerequisites_DeduplicatesIds()
    {
        Campaign campaign = CreateCampaign();
        Guid required = Guid.NewGuid();

        campaign.SetPrerequisites([required, required, required]);

        Assert.That(campaign.Prerequisites, Has.Count.EqualTo(1));
    }

    [Test]
    public void SetPrerequisites_IgnoresEmptyGuids()
    {
        Campaign campaign = CreateCampaign();
        Guid required = Guid.NewGuid();

        campaign.SetPrerequisites([Guid.Empty, required, Guid.Empty]);

        Assert.That(campaign.Prerequisites, Has.Count.EqualTo(1));
    }

    [Test]
    public void SetPrerequisites_ReplacesAnyPreviousSet()
    {
        Campaign campaign = CreateCampaign();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        campaign.SetPrerequisites([first]);
        campaign.SetPrerequisites([second]);

        Assert.That(campaign.Prerequisites.Single().RequiredCampaignId, Is.EqualTo(second));
    }

    [Test]
    public void SetPrerequisites_EmptyCollection_ClearsPrerequisites()
    {
        Campaign campaign = CreateCampaign();
        campaign.SetPrerequisites([Guid.NewGuid()]);

        campaign.SetPrerequisites([]);

        Assert.That(campaign.Prerequisites, Is.Empty);
    }

    [Test]
    public void SetPrerequisites_Null_Throws()
    {
        Campaign campaign = CreateCampaign();

        Assert.Throws<ArgumentNullException>(() => campaign.SetPrerequisites(null!));
    }

    [Test]
    public void SetPrerequisites_ContainingItself_Throws()
    {
        Campaign campaign = CreateCampaign();

        Assert.Throws<ArgumentException>(() => campaign.SetPrerequisites([campaign.Id]));
    }

    [Test]
    public void SetPrerequisites_SelfReferenceAmongOthers_ThrowsWithoutMutating()
    {
        Campaign campaign = CreateCampaign();
        campaign.SetPrerequisites([Guid.NewGuid()]);

        Assert.Throws<ArgumentException>(() => campaign.SetPrerequisites([Guid.NewGuid(), campaign.Id]));
        Assert.That(campaign.Prerequisites, Has.Count.EqualTo(1));
    }
}