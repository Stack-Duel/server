using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Domain.Tests.Campaigns.Entities;

public class CampaignModuleTests
{
    private static readonly int[] SequentialSortOrders = [0, 1, 2];

    private static CampaignModule CreateModule(int sortOrder = 0) =>
        new(Guid.NewGuid(), "Module 1", "First module.", sortOrder);

    [Fact]
    public void Constructor_SetsOwnerDetailsAndSortOrder()
    {
        Guid campaignId = Guid.NewGuid();

        CampaignModule module = new(campaignId, "Module 1", "First module.", 3);

        Assert.Equal(campaignId, module.CampaignId);
        Assert.Equal("Module 1", module.Title);
        Assert.Equal("First module.", module.Description);
        Assert.Equal(3, module.SortOrder);
        Assert.Empty(module.Units);
    }

    [Fact]
    public void Constructor_EmptyCampaignId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CampaignModule(Guid.Empty, "Module 1", "desc", 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new CampaignModule(Guid.NewGuid(), title!, "desc", 0));
    }

    [Fact]
    public void Constructor_TrimsTitle()
    {
        CampaignModule module = new(Guid.NewGuid(), "  Padded  ", "desc", 0);
        Assert.Equal("Padded", module.Title);
    }

    [Fact]
    public void Constructor_NullDescription_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CampaignModule(Guid.NewGuid(), "Module 1", null!, 0));
    }

    [Fact]
    public void UpdateDetails_ReplacesTitleAndDescription()
    {
        CampaignModule module = CreateModule();

        module.UpdateDetails("Renamed", "New description.");

        Assert.Equal("Renamed", module.Title);
        Assert.Equal("New description.", module.Description);
    }

    [Fact]
    public void UpdateDetails_BlankTitle_ThrowsWithoutMutating()
    {
        CampaignModule module = CreateModule();

        Assert.Throws<ArgumentException>(() => module.UpdateDetails("  ", "New description."));
        Assert.Equal("Module 1", module.Title);
    }

    [Fact]
    public void UpdateDetails_KeepsSortOrder()
    {
        CampaignModule module = CreateModule(sortOrder: 4);

        module.UpdateDetails("Renamed", "desc");

        Assert.Equal(4, module.SortOrder);
    }

    [Fact]
    public void AddUnit_AppendsUnitOwnedByTheModule()
    {
        CampaignModule module = CreateModule();

        CampaignUnit unit = module.AddUnit("Unit 1", "Lesson body.", UnitType.Lesson, 15);

        Assert.Single(module.Units);
        Assert.Equal(module.Id, unit.CampaignModuleId);
        Assert.Equal("Unit 1", unit.Title);
        Assert.Equal(UnitType.Lesson, unit.UnitType);
        Assert.Equal(15, unit.EstimatedMinutes);
    }

    [Fact]
    public void AddUnit_AssignsSequentialSortOrders()
    {
        CampaignModule module = CreateModule();

        CampaignUnit first = module.AddUnit("Unit 1", "", UnitType.Lesson, 5);
        CampaignUnit second = module.AddUnit("Unit 2", "", UnitType.Challenge, 5);
        CampaignUnit third = module.AddUnit("Unit 3", "", UnitType.Quiz, 5);

        Assert.Equal(SequentialSortOrders, new[] { first.SortOrder, second.SortOrder, third.SortOrder });
    }

    [Fact]
    public void AddUnit_InvalidUnit_DoesNotLeaveAPartialUnitBehind()
    {
        CampaignModule module = CreateModule();

        Assert.Throws<ArgumentException>(() => module.AddUnit("  ", "body", UnitType.Lesson, 5));
        Assert.Empty(module.Units);
    }
}