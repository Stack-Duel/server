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

    [Fact]
    public void Constructor_SetsDetails()
    {
        Campaign campaign = CreateCampaign(difficulty: CampaignDifficulty.Advanced);

        Assert.Equal("intro-to-algorithms", campaign.Slug);
        Assert.Equal("Intro to Algorithms", campaign.Title);
        Assert.Equal("Start here.", campaign.Description);
        Assert.Equal(CampaignDifficulty.Advanced, campaign.Difficulty);
    }

    [Fact]
    public void Constructor_StartsAsDraftWithNoPublishDate()
    {
        Campaign campaign = CreateCampaign();

        Assert.Equal(CampaignStatus.Draft, campaign.Status);
        Assert.Null(campaign.PublishedAt);
    }

    [Fact]
    public void Constructor_StartsWithNoIconSortOrderZeroAndNoChildren()
    {
        Campaign campaign = CreateCampaign();

        Assert.Null(campaign.IconKey);
        Assert.Equal(0, campaign.SortOrder);
        Assert.Empty(campaign.Modules);
        Assert.Empty(campaign.Prerequisites);
    }

    [Fact]
    public void Constructor_StampsCreatedAt()
    {
        DateTime before = DateTime.UtcNow;
        Assert.True(CreateCampaign().CreatedAt >= before);
    }

    [Theory]
    [InlineData("  Intro-To-Algorithms  ", "intro-to-algorithms")] // trimmed and lowercased
    [InlineData("ALGO", "algo")] // lowercased
    public void Constructor_NormalizesSlug(string slug, string expected)
    {
        Assert.Equal(expected, CreateCampaign(slug).Slug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankSlug_Throws(string? slug)
    {
        Assert.Throws<ArgumentException>(() => CreateCampaign(slug!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new Campaign("slug", title!, "desc", CampaignDifficulty.Beginner));
    }

    [Fact]
    public void Constructor_TrimsTitle()
    {
        Campaign campaign = new("slug", "  Padded  ", "desc", CampaignDifficulty.Beginner);
        Assert.Equal("Padded", campaign.Title);
    }

    [Fact]
    public void Constructor_NullDescription_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Campaign("slug", "Title", null!, CampaignDifficulty.Beginner));
    }

    [Fact]
    public void Constructor_EmptyDescription_IsAllowed()
    {
        Campaign campaign = new("slug", "Title", "", CampaignDifficulty.Beginner);
        Assert.Empty(campaign.Description);
    }

    [Fact]
    public void UpdateDetails_ReplacesTitleDescriptionDifficultyAndIcon()
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Renamed", "New description.", CampaignDifficulty.Expert, "icon-renamed");

        Assert.Equal("Renamed", campaign.Title);
        Assert.Equal("New description.", campaign.Description);
        Assert.Equal(CampaignDifficulty.Expert, campaign.Difficulty);
        Assert.Equal("icon-renamed", campaign.IconKey);
    }

    [Fact]
    public void UpdateDetails_TrimsIconKey()
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Title", "desc", CampaignDifficulty.Beginner, "  icon  ");

        Assert.Equal("icon", campaign.IconKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_BlankIconKey_IsStoredAsNull(string? iconKey)
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Title", "desc", CampaignDifficulty.Beginner, iconKey);

        Assert.Null(campaign.IconKey);
    }

    [Fact]
    public void UpdateDetails_DoesNotChangeTheSlug()
    {
        Campaign campaign = CreateCampaign();

        campaign.UpdateDetails("Renamed", "desc", CampaignDifficulty.Expert, null);

        Assert.Equal("intro-to-algorithms", campaign.Slug);
    }

    [Fact]
    public void Publish_FromDraft_SetsStatusAndPublishedAt()
    {
        Campaign campaign = CreateCampaign();
        DateTime before = DateTime.UtcNow;

        campaign.Publish();

        Assert.Equal(CampaignStatus.Published, campaign.Status);
        Assert.True(campaign.PublishedAt >= before);
    }

    [Fact]
    public void Publish_AlreadyPublished_Throws()
    {
        Campaign campaign = CreateCampaign();
        campaign.Publish();

        Assert.Throws<InvalidOperationException>(campaign.Publish);
    }

    [Fact]
    public void Publish_Archived_Throws()
    {
        Campaign campaign = CreateCampaign();
        campaign.Archive();

        Assert.Throws<InvalidOperationException>(campaign.Publish);
    }

    [Fact]
    public void Archive_FromDraft_SetsStatus()
    {
        Campaign campaign = CreateCampaign();

        campaign.Archive();

        Assert.Equal(CampaignStatus.Archived, campaign.Status);
    }

    [Fact]
    public void Archive_FromPublished_SetsStatusAndKeepsPublishedAt()
    {
        Campaign campaign = CreateCampaign();
        campaign.Publish();
        DateTime? publishedAt = campaign.PublishedAt;

        campaign.Archive();

        Assert.Equal(CampaignStatus.Archived, campaign.Status);
        Assert.Equal(publishedAt, campaign.PublishedAt);
    }

    [Fact]
    public void Archive_AlreadyArchived_Throws()
    {
        Campaign campaign = CreateCampaign();
        campaign.Archive();

        Assert.Throws<InvalidOperationException>(campaign.Archive);
    }

    [Fact]
    public void SetSortOrder_StoresValue()
    {
        Campaign campaign = CreateCampaign();

        campaign.SetSortOrder(7);

        Assert.Equal(7, campaign.SortOrder);
    }

    [Fact]
    public void AddModule_AppendsModuleOwnedByTheCampaign()
    {
        Campaign campaign = CreateCampaign();

        CampaignModule module = campaign.AddModule("Module 1", "First module.");

        Assert.Single(campaign.Modules);
        Assert.Equal(campaign.Id, module.CampaignId);
        Assert.Equal("Module 1", module.Title);
    }

    [Fact]
    public void AddModule_AssignsSequentialSortOrders()
    {
        Campaign campaign = CreateCampaign();

        CampaignModule first = campaign.AddModule("Module 1", "");
        CampaignModule second = campaign.AddModule("Module 2", "");
        CampaignModule third = campaign.AddModule("Module 3", "");

        Assert.Equal(SequentialSortOrders, new[] { first.SortOrder, second.SortOrder, third.SortOrder });
    }

    [Fact]
    public void SetPrerequisites_AddsOnePrerequisitePerId()
    {
        Campaign campaign = CreateCampaign();
        Guid requiredA = Guid.NewGuid();
        Guid requiredB = Guid.NewGuid();

        campaign.SetPrerequisites([requiredA, requiredB]);

        Assert.Equivalent(
            new[] { requiredA, requiredB },
            campaign.Prerequisites.Select(p => p.RequiredCampaignId),
            strict: true
        );
        Assert.All(campaign.Prerequisites.Select(p => p.CampaignId), item => Assert.Equal(campaign.Id, item));
    }

    [Fact]
    public void SetPrerequisites_DeduplicatesIds()
    {
        Campaign campaign = CreateCampaign();
        Guid required = Guid.NewGuid();

        campaign.SetPrerequisites([required, required, required]);

        Assert.Single(campaign.Prerequisites);
    }

    [Fact]
    public void SetPrerequisites_IgnoresEmptyGuids()
    {
        Campaign campaign = CreateCampaign();
        Guid required = Guid.NewGuid();

        campaign.SetPrerequisites([Guid.Empty, required, Guid.Empty]);

        Assert.Single(campaign.Prerequisites);
    }

    [Fact]
    public void SetPrerequisites_ReplacesAnyPreviousSet()
    {
        Campaign campaign = CreateCampaign();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        campaign.SetPrerequisites([first]);
        campaign.SetPrerequisites([second]);

        Assert.Equal(second, campaign.Prerequisites.Single().RequiredCampaignId);
    }

    [Fact]
    public void SetPrerequisites_EmptyCollection_ClearsPrerequisites()
    {
        Campaign campaign = CreateCampaign();
        campaign.SetPrerequisites([Guid.NewGuid()]);

        campaign.SetPrerequisites([]);

        Assert.Empty(campaign.Prerequisites);
    }

    [Fact]
    public void SetPrerequisites_Null_Throws()
    {
        Campaign campaign = CreateCampaign();

        Assert.Throws<ArgumentNullException>(() => campaign.SetPrerequisites(null!));
    }

    [Fact]
    public void SetPrerequisites_ContainingItself_Throws()
    {
        Campaign campaign = CreateCampaign();

        Assert.Throws<ArgumentException>(() => campaign.SetPrerequisites([campaign.Id]));
    }

    [Fact]
    public void SetPrerequisites_SelfReferenceAmongOthers_ThrowsWithoutMutating()
    {
        Campaign campaign = CreateCampaign();
        campaign.SetPrerequisites([Guid.NewGuid()]);

        Assert.Throws<ArgumentException>(() => campaign.SetPrerequisites([Guid.NewGuid(), campaign.Id]));
        Assert.Single(campaign.Prerequisites);
    }
}